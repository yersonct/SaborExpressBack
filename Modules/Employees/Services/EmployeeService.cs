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
            ILogger<EmployeeService> logger
            )
        {
            _deliveryAssignmentService = deliveryAssignmentService;
            _logger = logger;
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

            var esGerente = EsGerente(currentUser);

            List<EmployeeListItem> employees = (esGerente || currentUser.Employee?.BranchId == null)
                ? await _employeeRepository.GetAllLightAsync(estado)
                : await _employeeRepository.GetAllByBranchLightAsync(currentUser.Employee.BranchId.Value, estado);

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
                await _refreshTokenRepository.RevokeAllForUserAsync(employee.UserId);

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

            employee.Status = "Retirado";

            // Le quita los roles operativos (Mesero, Cocinero, etc.), deja solo Cliente si lo tiene
            if (employee.User != null)
            {
                employee.User.UserRoles.RemoveAll(ur => ur.Role.Name != RoleNames.Cliente);
            }

            await _employeeRepository.UpdateAsync(employee);
        }

        public async Task<(byte[] Archivo, string NombreArchivo, string ContentType)?> GetCvAsync(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee?.CvFile == null)
                return null;

            return (employee.CvFile, employee.CvFilename ?? "cv.pdf", employee.CvContentType ?? "application/pdf");
        }
        public async Task<EmployeeMeResponseDto> GetMeAsync(int employeeId)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException("Empleado no encontrado.");

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var tablesAttendedToday = await _context.Orders
                .Where(o => o.EmployeeId == employeeId
                    && o.CreatedAt >= today
                    && o.CreatedAt < tomorrow
                    && o.Status != OrderStatus.Cancelled)
                .CountAsync();

            var paymentsToday = await _context.Payments
                .Where(p => p.CashierId == employeeId
                    && p.PaidAt >= today
                    && p.PaidAt < tomorrow
                    && p.Status == SaborExpress.Modules.Payments.Enum.PaymentStatus.Completed)
                .ToListAsync();

            // NUEVO — Paso 6: viajes completados (histórico, no solo hoy).
            var tripsCompleted = await _context.Deliveries
                .Where(d => d.DeliveryPersonId == employeeId && d.Status == DeliveryStatus.Delivered)
                .CountAsync();

            // Promedio de TODAS las calificaciones que los clientes le dieron a este
            // repartidor. Si aún no tiene ninguna, queda null (la app muestra "—").
            var averageRating = await _context.Deliveries
                .Where(d => d.DeliveryPersonId == employeeId && d.CustomerRating != null)
                .AverageAsync(d => (double?)d.CustomerRating);

            if (averageRating.HasValue)
                averageRating = Math.Round(averageRating.Value, 1);

            return new EmployeeMeResponseDto
            {
                Id = employee.Id,
                Name = employee.Name,
                LastName = employee.LastName,
                Email = employee.User?.Email,
                Phone = employee.Phone,
                BranchId = employee.BranchId,
                BranchName = employee.Branch?.Name,
                RoleNames = employee.User?.UserRoles.Select(ur => ur.Role.Name).ToList() ?? new(),
                TablesAttendedToday = tablesAttendedToday,
                IsAvailable = employee.IsAvailable,
                PaymentsCollectedToday = paymentsToday.Count,
                AmountCollectedToday = paymentsToday.Sum(p => p.Amount),
                Vehicle = employee.Vehicle,
                Plate = employee.Plate,
                TripsCompleted = tripsCompleted,
                AverageRating = averageRating
            };
        }
        public async Task<EmployeeMeResponseDto> SetMyAvailabilityAsync(int employeeId, bool isAvailable)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException("Empleado no encontrado.");

            employee.IsAvailable = isAvailable;
            await _employeeRepository.UpdateAsync(employee);

            // Si se puso disponible, reintenta asignar pedidos que quedaron sin repartidor
            var isRepartidor = employee.User?.UserRoles.Any(ur => ur.Role.Name == RoleNames.Repartidor) ?? false;
            if (isAvailable && isRepartidor && employee.BranchId.HasValue)
            {
                try
                {
                    await _deliveryAssignmentService.AssignPendingForBranchAsync(employee.BranchId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudieron reasignar pedidos pendientes de la sede {BranchId}", employee.BranchId);
                }
            }

            return await GetMeAsync(employeeId);
        }
        public async Task<EmployeeMeResponseDto> UpdateMeAsync(int employeeId, UpdateEmployeeMeDto dto)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId)
                ?? throw new KeyNotFoundException("Empleado no encontrado.");

            employee.Name = dto.Name;
            employee.LastName = dto.LastName;
            employee.Phone = dto.Phone;
            employee.Vehicle = dto.Vehicle;
            employee.Plate = dto.Plate;

            await _employeeRepository.UpdateAsync(employee);

            return await GetMeAsync(employeeId);
        }
        private static void EnsureSameBranch(User currentUser, Employee target)
        {
            if (EsGerente(currentUser))
                return;

            if (currentUser.Employee?.BranchId != target.BranchId)
                throw new InvalidOperationException("Solo puedes gestionar empleados de tu propia sede.");
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