using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Employees.DTOs;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Employees.Mappings;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Employees.Validators;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Helpers;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Shared.Exceptions;
using SaborExpress.Data; 
using Microsoft.EntityFrameworkCore;
using SaborExpress.Modules.Orders.Enum; 
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Enum;

namespace SaborExpress.Modules.Employees.Services
{
 public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly EmployeeValidator _employeeValidator;
        private readonly IAuthRepository _authRepository;
        private readonly INotificationService _notificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IEmployeeActivationService _employeeActivationService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly AppDbContext _context;
        private readonly IDeliveryAssignmentService _deliveryAssignmentService;
        private readonly ILogger<EmployeeService> _logger;
        private readonly IRealtimeNotifier _realtime;
        public EmployeeService(
            IEmployeeRepository employeeRepository,
            EmployeeValidator employeeValidator,
            IAuthRepository authRepository,
            INotificationService notificationService,
            IAuthorizationService authorizationService,
            IEmployeeActivationService employeeActivationService,
            IRefreshTokenRepository refreshTokenRepository,
            AppDbContext context,
            IDeliveryAssignmentService deliveryAssignmentService,
            ILogger<EmployeeService> logger,
IRealtimeNotifier realtime
            )
        {
            _deliveryAssignmentService = deliveryAssignmentService;
            _logger = logger;
_realtime = realtime;
            _employeeRepository = employeeRepository;
            _employeeValidator = employeeValidator;
            _authRepository = authRepository;
            _notificationService = notificationService;
            _authorizationService = authorizationService;
            _employeeActivationService = employeeActivationService;
            _refreshTokenRepository = refreshTokenRepository;
            _context = context; 
        }

        public async Task<List<EmployeeResponseDto>> GetAllAsync(int currentUserId, string estado = "activo")
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            List<EmployeeListItem> employees;

            if (EsGerente(currentUser))
            {
                employees = await _employeeRepository.GetAllLightAsync(estado);
            }
            else
            {
                var branchId = currentUser.Employee?.BranchId
                    ?? throw new ForbiddenException("El administrador debe tener una sede asignada.");

                employees = await _employeeRepository.GetAllByBranchLightAsync(branchId, estado);
            }

            return employees.Select(EmployeeMapper.ToResponse).ToList();
        }

        public async Task<EmployeeResponseDto> GetByIdAsync(int id, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var employee = await _employeeRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El empleado no existe");

            EnsureSameBranch(currentUser, employee);

            return EmployeeMapper.ToResponse(employee);
        }

        public async Task<EmployeeResponseDto> CreateAsync(CreateEmployeeDto dto, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (!EsGerente(currentUser) &&
                (currentUser.Employee?.BranchId is not int branchId || dto.BranchId != branchId))
            {
                throw new ForbiddenException("Solo puedes crear empleados de tu propia sede.");
            }

            var currentEmployeeId = currentUser.Employee?.Id
                ?? throw new InvalidOperationException("El usuario actual no tiene un perfil de empleado asociado.");

            if (!await _authorizationService.CanPerformActionAsync(currentEmployeeId, PermissionNames.CrearEmpleado))
                throw new InvalidOperationException("No tienes permiso para crear empleados ahora mismo.");

            await _employeeValidator.ValidateCreateAsync(dto);

            var employee = new Employee
            {
                Name = dto.Name,
                LastName = dto.LastName,
                Document = dto.Document,
                Phone = dto.Phone,
                Address = dto.Address,
                BranchId = dto.BranchId,
                Status = "Activo",
                BasePay = dto.BasePay,
                User = new User
                {
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                    Status = false,
                    EmailConfirmed = false,
                    MustChangePassword = false
                }
            };



            if (dto.Cv != null && dto.Cv.Length > 0)
                await UpdateCvAsync(employee, dto.Cv);

            await _employeeRepository.AddAsync(employee);

            await _employeeActivationService.SendActivationCodeAsync(
                employee.User.Id, employee.User.Email!, employee.Name);

            return EmployeeMapper.ToResponse(employee);
        }

        public async Task<EmployeeResponseDto> UpdateAsync(int id, UpdateEmployeeDto dto, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var employee = await _employeeRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El empleado no existe");

            EnsureSameBranch(currentUser, employee);

            if (!EsGerente(currentUser) && employee.BranchId != dto.BranchId)
                throw new InvalidOperationException("Solo el Gerente puede cambiar la sede de un empleado.");

            var hasExistingCv = employee.CvFile != null && employee.CvFile.Length > 0;   // 👈 nuevo
            await _employeeValidator.ValidateUpdateAsync(dto, employee.UserId, hasExistingCv);   // 👈 se agrega el parámetro

            var branchChanged = employee.BranchId != dto.BranchId; // 👈 nuevo: lo detectamos ANTES de sobrescribir

            UpdatePersonalData(employee, dto);

            if (employee.User != null)
            {
                UpdateEmail(employee.User, dto.Email);
            }

            if (dto.Cv != null && dto.Cv.Length > 0)
                await UpdateCvAsync(employee, dto.Cv);

            await _employeeRepository.UpdateAsync(employee);

            // 👇 nuevo: si cambió de sede, su token viejo tiene el BranchId
            // desactualizado — lo forzamos a re-loguearse revocando sus refresh-tokens.
            if (branchChanged)
            {
                // Los turnos de hoy en adelante acompañan al empleado a su nueva sede;
                // los anteriores permanecen en la sede donde ocurrieron.
                if (dto.BranchId.HasValue)
                {
                    var today = DateOnly.FromDateTime(ColombiaTime.Now);
                    var futureSchedules = await _context.EmployeeSchedules
                        .Where(s => s.EmployeeId == id && s.ShiftDate >= today)
                        .ToListAsync();

                    foreach (var schedule in futureSchedules)
                        schedule.BranchId = dto.BranchId.Value;

                    if (futureSchedules.Count > 0)
                        await _context.SaveChangesAsync();
                }

                await _refreshTokenRepository.RevokeAllForUserAsync(employee.UserId);
                await _authRepository.ClearSessionTokensAsync(new[] { employee.UserId });
                await _realtime.ForceLogoutAsync(employee.UserId, "branch-changed");
            }

            return EmployeeMapper.ToResponse(employee);
        }

        public async Task DeactivateAsync(int id, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var currentEmployeeId = currentUser.Employee?.Id
                ?? throw new InvalidOperationException("El usuario actual no tiene un perfil de empleado asociado.");

            if (!await _authorizationService.CanPerformActionAsync(currentEmployeeId, PermissionNames.EliminarEmpleado))
                throw new InvalidOperationException("No tienes permiso para eliminar empleados ahora mismo.");

            var employee = await _employeeRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El empleado no existe");

            EnsureSameBranch(currentUser, employee);
            employee.Status = "Retirado";

            // Le quita los roles operativos (Mesero, Cocinero, etc.), deja solo Cliente si lo tiene
            if (employee.User != null)
            {
                employee.User.UserRoles.RemoveAll(ur => ur.Role.Name != RoleNames.Cliente);
            }

            await _employeeRepository.UpdateAsync(employee);
            await _refreshTokenRepository.RevokeAllForUserAsync(employee.UserId);
            await _authRepository.ClearSessionTokensAsync(new[] { employee.UserId });
            await _realtime.ForceLogoutAsync(employee.UserId, "employee-suspended");
        }

        public async Task DeletePermanentlyAsync(int id, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var canDeleteEmployees = EsGerente(currentUser)
                || currentUser.UserRoles.Any(ur => ur.Role.Name == RoleNames.Administrador);
            if (!canDeleteEmployees)
                throw new InvalidOperationException("Solo un gerente o administrador puede eliminar empleados.");

            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            var employee = await _employeeRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El empleado no existe.");

            EnsureSameBranch(currentUser, employee);

            if (employee.UserId == currentUserId)
                throw new InvalidOperationException("No puedes eliminar tu propia cuenta.");

            if (await _context.Customers.AnyAsync(c => c.UserId == employee.UserId))
                throw new InvalidOperationException("Esta cuenta también está registrada como cliente. Suspéndela para conservar sus datos de cliente.");

            var deliveries = await _context.Deliveries
                .Where(d => d.DeliveryPersonId == id)
                .ToListAsync();
            if (deliveries.Any(d =>
                    d.Status == DeliveryStatus.Assigned || d.Status == DeliveryStatus.InTransit))
                throw new InvalidOperationException("Reasigna o finaliza las entregas activas antes de eliminar al empleado.");

            var orders = await _context.Orders
                .Where(o => o.EmployeeId == id)
                .ToListAsync();
            if (orders.Any(o =>
                    o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled))
                throw new InvalidOperationException("Finaliza o reasigna los pedidos activos antes de eliminar al empleado.");

            var schedules = await _context.EmployeeSchedules
                .Where(s => s.EmployeeId == id)
                .ToListAsync();
            _context.EmployeeSchedules.RemoveRange(schedules);

            _context.Deliveries.RemoveRange(deliveries);

            foreach (var order in orders)
                order.EmployeeId = null;

            var payments = await _context.Payments
                .Where(p => p.CashierId == id)
                .ToListAsync();
            foreach (var payment in payments)
                payment.CashierId = null;

            var orderDetails = await _context.OrderDetails
                .Where(d => d.LastModifiedByEmployeeId == id)
                .ToListAsync();
            foreach (var detail in orderDetails)
                detail.LastModifiedByEmployeeId = null;

            var statusHistories = await _context.OrderStatusHistories
                .Where(h => h.ChangedByEmployeeId == id)
                .ToListAsync();
            foreach (var history in statusHistories)
                history.ChangedByEmployeeId = null;

            var detailHistories = await _context.OrderDetailHistories
                .Where(h => h.ChangedByEmployeeId == id)
                .ToListAsync();
            foreach (var history in detailHistories)
                history.ChangedByEmployeeId = null;

            // Retiene los pedidos y pagos como historial, sin conservar la asociación personal.
            await _context.SaveChangesAsync();

            _context.Users.Remove(employee.User);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _realtime.ForceLogoutAsync(employee.UserId, "employee-deleted");
        }

        public async Task<(byte[] Archivo, string NombreArchivo, string ContentType)?> GetCvAsync(
            int id, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var employee = await _employeeRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El empleado no existe.");

            EnsureSameBranch(currentUser, employee);

            if (employee.CvFile == null || employee.CvFile.Length == 0)
                return null;

            return (
                employee.CvFile,
                employee.CvFilename ?? "cv.pdf",
                employee.CvContentType ?? "application/pdf");
        }
        public async Task<EmployeeMeResponseDto> GetMeAsync(int employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException("Empleado no encontrado.");

            var todayInColombia = ColombiaTime.Now.Date;
            var todayUtc = ColombiaTime.ToUtc(todayInColombia);
            var tomorrowUtc = ColombiaTime.ToUtc(todayInColombia.AddDays(1));

            var tablesAttendedToday = await _context.Orders
                .Where(o => o.EmployeeId == employeeId
                    && o.TableId.HasValue
                    && o.OrderType == OrderType.DineIn
                    && o.CreatedAt >= todayUtc
                    && o.CreatedAt < tomorrowUtc
                    && o.Status != OrderStatus.Cancelled)
                .CountAsync();

            var toGoOrdersToday = await _context.Orders
                .Where(o => o.EmployeeId == employeeId
                    && o.OrderType == OrderType.ToGo
                    && o.CreatedAt >= todayUtc
                    && o.CreatedAt < tomorrowUtc
                    && o.Status != OrderStatus.Cancelled)
                .CountAsync();

            var paymentsToday = await _context.Payments
                .Where(p => p.CashierId == employeeId
                    && p.PaidAt >= todayUtc
                    && p.PaidAt < tomorrowUtc
                    && p.Status == SaborExpress.Modules.Payments.Enum.PaymentStatus.Completed)
                .ToListAsync();

            // El perfil de domicilio muestra solo actividad de hoy en hora Colombia.
            var tripsCompleted = await _context.Deliveries
                .Where(d => d.DeliveryPersonId == employeeId
                    && d.Status == DeliveryStatus.Delivered
                    && d.DeliveredAt >= todayUtc
                    && d.DeliveredAt < tomorrowUtc)
                .CountAsync();

            // Promedio de las calificaciones recibidas hoy. Si aún no tiene ninguna,
            // queda null (la app muestra "—").
            var averageRating = await _context.Deliveries
                .Where(d => d.DeliveryPersonId == employeeId
                    && d.CustomerRating != null
                    && d.RatedAt >= todayUtc
                    && d.RatedAt < tomorrowUtc)
                .AverageAsync(d => (double?)d.CustomerRating);

            if (averageRating.HasValue)
                averageRating = Math.Round(averageRating.Value, 1);

            // Sede del turno de HOY (hora Colombia). Si hay varios: el activo ahora,
            // si no el próximo, si no el último. Sin turno hoy: la sede base.
            var nowCo = ColombiaTime.Now;
            var todayCo = DateOnly.FromDateTime(nowCo);
            var nowTimeCo = TimeOnly.FromDateTime(nowCo);

            var todayShifts = await _context.EmployeeSchedules
                .Where(s => s.EmployeeId == employeeId && s.ShiftDate == todayCo)
                .Select(s => new
                {
                    s.BranchId,
                    BranchName = s.Branch.Name,
                    RoleName = s.Role.Name,
                    s.StartTime,
                    s.EndTime
                })
                .ToListAsync();

            var activeShift   = todayShifts.FirstOrDefault(s => s.StartTime <= nowTimeCo && nowTimeCo <= s.EndTime);
            var upcomingShift = todayShifts.Where(s => s.StartTime > nowTimeCo).OrderBy(s => s.StartTime).FirstOrDefault();
            var lastShift     = todayShifts.OrderByDescending(s => s.EndTime).FirstOrDefault();

            var currentShift = activeShift ?? upcomingShift ?? lastShift; // para la sede
            var visibleShift = activeShift ?? upcomingShift;              // para el rol

            var shiftState =
                activeShift   != null ? "Active"   :
                upcomingShift != null ? "Upcoming" :
                lastShift     != null ? "Ended"    : "None";

            return new EmployeeMeResponseDto
            {
                Id = employee.Id,
                Name = employee.Name,
                LastName = employee.LastName,
                Email = employee.User?.Email,
                Phone = employee.Phone,
                BranchId = currentShift?.BranchId ?? employee.BranchId,
                BranchName = currentShift?.BranchName ?? employee.Branch?.Name,
                RoleNames = employee.User?.UserRoles.Select(ur => ur.Role.Name).ToList() ?? new(),
                TablesAttendedToday = tablesAttendedToday,
                ToGoOrdersToday = toGoOrdersToday,
                IsAvailable = employee.IsAvailable,
                PaymentsCollectedToday = paymentsToday.Count,
                AmountCollectedToday = paymentsToday.Sum(p => p.Amount),
                TripsCompleted = tripsCompleted,
                AverageRating = averageRating,
                TodayRoleName = visibleShift?.RoleName,
                TodayShiftState = shiftState
            };
        }
        public async Task<EmployeeMeResponseDto> SetMyAvailabilityAsync(int employeeId, bool isAvailable)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException("Empleado no encontrado.");

            var isRepartidor = employee.User?.UserRoles.Any(ur => ur.Role.Name == RoleNames.Repartidor) ?? false;
            employee.IsAvailable = isAvailable;
            // Cada heartbeat desde la app mantiene la presencia viva. Al salir
            // de la app, el vencimiento automático evita asignaciones a ausentes.
            if (isRepartidor)
                employee.LastCourierHeartbeatAt = isAvailable ? DateTime.UtcNow : null;
            await _employeeRepository.UpdateAsync(employee);

            // Solo los repartidores reactivan la cola, sin modificar otros roles.
            if (isAvailable && isRepartidor && employee.BranchId.HasValue)
            {
                try
                {
                    await _deliveryAssignmentService.AssignPendingForBranchAsync(employee.BranchId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError("No se pudieron reasignar pedidos pendientes de la sede ({ErrorType}).", ex.GetType().Name);
                }
            }

            return await GetMeAsync(employeeId);
        }

        public async Task UpdateMyLocationAsync(int employeeId, decimal latitude, decimal longitude)
        {
            if (latitude is < -90 or > 90)
                throw new ArgumentException("La latitud debe estar entre -90 y 90.");
            if (longitude is < -180 or > 180)
                throw new ArgumentException("La longitud debe estar entre -180 y 180.");

            var employee = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException("Empleado no encontrado.");

            var isCourier = employee.User?.UserRoles.Any(ur => ur.Role.Name == RoleNames.Repartidor) ?? false;
            if (!isCourier)
                throw new UnauthorizedAccessException("Solo un repartidor puede reportar su ubicación.");

            var now = DateTime.UtcNow;
            var locationWasStale = !employee.LocationUpdatedAt.HasValue
                || employee.LocationUpdatedAt.Value < now.AddMinutes(-5);
            employee.CurrentLatitude = latitude;
            employee.CurrentLongitude = longitude;
            employee.LocationUpdatedAt = now;
            await _employeeRepository.UpdateAsync(employee);

            if (employee.IsAvailable && employee.BranchId.HasValue && locationWasStale)
            {
                try
                {
                    await _deliveryAssignmentService.AssignPendingForBranchAsync(employee.BranchId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError("No se pudieron asignar pedidos pendientes después de recibir ubicación del repartidor ({ErrorType}).", ex.GetType().Name);
                }
            }

            var trackingRecipients = await (
                from delivery in _context.Deliveries
                join order in _context.Orders on delivery.OrderId equals order.Id
                join customer in _context.Customers on order.CustomerId equals customer.Id
                where delivery.DeliveryPersonId == employeeId
                    && delivery.Status == DeliveryStatus.InTransit
                    && order.Status != OrderStatus.Cancelled
                select new { delivery.Id, delivery.OrderId, customer.UserId }
            ).ToListAsync();

            await Task.WhenAll(trackingRecipients.Select(recipient =>
                _realtime.DeliveryLocationUpdatedAsync(
                    recipient.UserId,
                    recipient.OrderId,
                    recipient.Id,
                    latitude,
                    longitude,
                    now)));
        }

        public async Task<EmployeeMeResponseDto> UpdateMeAsync(int employeeId, UpdateEmployeeMeDto dto)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException("Empleado no encontrado.");

            employee.Name = dto.Name;
            employee.LastName = dto.LastName;
            employee.Phone = dto.Phone;
            await _employeeRepository.UpdateAsync(employee);

            return await GetMeAsync(employeeId);
        }
        private static void EnsureSameBranch(User currentUser, Employee target)
        {
            if (EsGerente(currentUser))
                return;

            if (currentUser.Employee?.BranchId is not int branchId || target.BranchId != branchId)
                throw new ForbiddenException("Solo puedes gestionar empleados de tu propia sede.");
        }

        private static bool EsGerente(User user) =>
            user.UserRoles.Any(ur => ur.Role.Name == RoleNames.Gerente);

        private static void UpdatePersonalData(Employee employee, UpdateEmployeeDto dto)
        {
            employee.Name = dto.Name;
            employee.LastName = dto.LastName;
            employee.Phone = dto.Phone;
            employee.Address = dto.Address;
            employee.BranchId = dto.BranchId;
            employee.Status = dto.Status;
            employee.BasePay = dto.BasePay;
        }

        private static void UpdateEmail(User user, string? email)
        {
            if (!string.IsNullOrWhiteSpace(email))
                user.Email = email;
        }



        private static async Task UpdateCvAsync(Employee employee, IFormFile cv)
        {
            employee.CvFile = await FileHelper.ToByteArrayAsync(cv);
            employee.CvFilename = cv.FileName;
            employee.CvContentType = cv.ContentType;
        }
    }
}
