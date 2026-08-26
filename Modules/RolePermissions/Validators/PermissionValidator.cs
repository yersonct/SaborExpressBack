using SaborExpress.Modules.Permissions.Interfaces;
using SaborExpress.Modules.RolePermissions.DTOs;
using SaborExpress.Modules.RolePermissions.Interfaces;
using SaborExpress.Modules.Roles.Interfaces;

namespace SaborExpress.Modules.RolePermissions.Validators
{
    public class RolePermissionValidator
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IRolePermissionRepository _rolePermissionRepository;

        public RolePermissionValidator(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IRolePermissionRepository rolePermissionRepository)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
        }

        public async Task ValidateAssignAsync(CreateRolePermissionDto dto)
        {
            var role = await _roleRepository.GetByIdAsync(dto.RoleId)
                ?? throw new KeyNotFoundException("El rol no existe.");

            var permission = await _permissionRepository.GetByIdAsync(dto.PermissionId)
                ?? throw new KeyNotFoundException("El permiso no existe.");

            if (await _rolePermissionRepository.ExistsAsync(dto.RoleId, dto.PermissionId))
                throw new ArgumentException(
                    $"El rol '{role.Name}' ya tiene asignado el permiso '{permission.Name}'.");
        }

        public async Task ValidateRemoveAsync(int roleId, int permissionId)
        {
            var exists = await _rolePermissionRepository.GetAsync(roleId, permissionId);
            if (exists is null)
                throw new KeyNotFoundException("Esa relación rol-permiso no existe.");
        }
    }
}