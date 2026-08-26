using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.Employees.DTOs;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Roles.Interfaces;
using SaborExpress.Modules.Roles.Models;
using Microsoft.AspNetCore.Http;

namespace SaborExpress.Modules.Employees.Validators
{
    public class EmployeeValidator
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBranchRepository _branchRepository;
        private const long MaxCvSizeBytes = 5 * 1024 * 1024;

        public EmployeeValidator(
            IEmployeeRepository employeeRepository,
            IAuthRepository authRepository,
            IRoleRepository roleRepository,
            IBranchRepository branchRepository)
        {
            _employeeRepository = employeeRepository;
            _authRepository = authRepository;
            _roleRepository = roleRepository;
            _branchRepository = branchRepository;
        }

        public async Task ValidateCreateAsync(CreateEmployeeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre completo es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.Document))
                throw new ArgumentException("El documento es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("El correo es obligatorio");

            if (dto.RoleIds == null || dto.RoleIds.Count == 0)
                throw new ArgumentException("Debe asignar al menos un rol al empleado");

            if (await _employeeRepository.ExistsByDocumentAsync(dto.Document))
                throw new ArgumentException("Ya existe un empleado con ese documento");

            if (await _authRepository.ExistsByEmailAsync(dto.Email))
                throw new ArgumentException("Ya existe un usuario registrado con ese correo");

            var roles = await ValidateRoleIdsExistAsync(dto.RoleIds);
            await ValidateBranchExistsAsync(dto.BranchId);

            ValidateCvFile(dto.Cv);

            // En creación, nunca hay un CV existente todavía -> siempre se exige si el rol lo requiere.
            ValidateCvRequirement(roles, dto.Cv, hasExistingCv: false);
        }

        // Nuevo parámetro: hasExistingCv. Lo llena EmployeeService, que ya tiene
        // cargado el Employee (y por tanto sabe si employee.CvFile != null) antes
        // de llamar a este validador.
        public async Task ValidateUpdateAsync(UpdateEmployeeDto dto, int currentUserId, bool hasExistingCv)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre completo es obligatorio");

            if (dto.RoleIds == null || dto.RoleIds.Count == 0)
                throw new ArgumentException("Debe asignar al menos un rol al empleado");

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                await _authRepository.ExistsByEmailAsync(dto.Email, currentUserId))
                throw new ArgumentException("Ya existe un usuario registrado con ese correo");

            var roles = await ValidateRoleIdsExistAsync(dto.RoleIds);
            await ValidateBranchExistsAsync(dto.BranchId);

            ValidateCvFile(dto.Cv);
            ValidateCvRequirement(roles, dto.Cv, hasExistingCv);
        }

        private async Task<List<Role>> ValidateRoleIdsExistAsync(List<int> roleIds)
        {
            var uniqueIds = roleIds.Distinct().ToList();
            var roles = await _roleRepository.GetByIdsAsync(uniqueIds);

            if (roles.Count != uniqueIds.Count)
                throw new ArgumentException("Uno o mas de los roles especificados no existen.");

            return roles;
        }

        private async Task ValidateBranchExistsAsync(int? branchId)
        {
            if (branchId.HasValue && await _branchRepository.GetByIdAsync(branchId.Value) == null)
                throw new ArgumentException("La sede especificada no existe.");
        }

        private static void ValidateCvFile(IFormFile? cv)
        {
            if (cv == null) return;

            if (cv.ContentType != "application/pdf")
                throw new ArgumentException("El CV debe ser un archivo PDF");

            if (cv.Length > MaxCvSizeBytes)
                throw new ArgumentException("El PDF no debe superar 5MB");
        }

        private static void ValidateCvRequirement(List<Role> roles, IFormFile? cv, bool hasExistingCv)
        {
            var requiereCv = roles.Any(r => r.RequiresCv);
            var yaTieneCvGuardado = hasExistingCv;
            var estaSubiendoCvNuevo = cv != null && cv.Length > 0;

            // Solo bloquea si el rol lo exige, Y no viene un CV nuevo en este request,
            // Y tampoco tiene uno ya guardado de antes.
            if (requiereCv && !estaSubiendoCvNuevo && !yaTieneCvGuardado)
            {
                var rolesQueLoExigen = string.Join(", ", roles.Where(r => r.RequiresCv).Select(r => r.Name));
                throw new ArgumentException(
                    $"El CV es obligatorio para el/los rol(es) seleccionado(s): {rolesQueLoExigen}.");
            }
        }
    }
}