using SaborExpress.Modules.Auth.Helpers;
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

        public EmployeeService(
            IEmployeeRepository employeeRepository,
            EmployeeValidator employeeValidator,
            IAuthRepository authRepository,
            INotificationService notificationService,
            IAuthorizationService authorizationService,
            IEmployeeActivationService employeeActivationService
            )
        {
            _employeeRepository = employeeRepository;
            _employeeValidator = employeeValidator;
            _authRepository = authRepository;
            _notificationService = notificationService;
            _authorizationService = authorizationService;
            _employeeActivationService = employeeActivationService;
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

    foreach (var roleId in dto.RoleIds)
        employee.User.UserRoles.Add(new UserRole { RoleId = roleId });

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

            UpdatePersonalData(employee, dto);

            if (employee.User != null)
            {
                UpdateEmail(employee.User, dto.Email);
                UpdateRoles(employee.User, dto.RoleIds);
            }

            if (dto.Cv != null && dto.Cv.Length > 0)
                await UpdateCvAsync(employee, dto.Cv);

            await _employeeRepository.UpdateAsync(employee);
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

        private static void UpdateRoles(User user, List<int> roleIds)
        {
            user.UserRoles.Clear();
            foreach (var roleId in roleIds)
                user.UserRoles.Add(new UserRole { RoleId = roleId, UserId = user.Id });
        }

        private static async Task UpdateCvAsync(Employee employee, IFormFile cv)
        {
            employee.CvFile = await FileHelper.ToByteArrayAsync(cv);
            employee.CvFilename = cv.FileName;
            employee.CvContentType = cv.ContentType;
        }
    }
}