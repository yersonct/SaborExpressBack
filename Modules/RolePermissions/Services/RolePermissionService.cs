using System.Security.Claims;
using SaborExpress.Modules.RolePermissions.DTOs;
using SaborExpress.Modules.RolePermissions.Interfaces;
using SaborExpress.Modules.RolePermissions.Mappings;
using SaborExpress.Modules.RolePermissions.Models;
using SaborExpress.Modules.RolePermissions.Validators;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Exceptions;

namespace SaborExpress.Modules.RolePermissions.Services
{
    public class RolePermissionService : IRolePermissionService
    {
        private readonly IRolePermissionRepository _rolePermissionRepository;
        private readonly RolePermissionValidator _rolePermissionValidator;

        public RolePermissionService(
            IRolePermissionRepository rolePermissionRepository,
            RolePermissionValidator rolePermissionValidator)
        {
            _rolePermissionRepository = rolePermissionRepository;
            _rolePermissionValidator = rolePermissionValidator;
        }

        public async Task<RolePermissionResponseDto> AssignAsync(CreateRolePermissionDto dto, ClaimsPrincipal actor)
        {
            EnsureCanManageGlobalPermissions(actor);
            await _rolePermissionValidator.ValidateAssignAsync(dto);

            var rolePermission = new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId,
                AssignedAt = DateTime.UtcNow
            };

            await _rolePermissionRepository.AddAsync(rolePermission);

            var created = await _rolePermissionRepository.GetAsync(dto.RoleId, dto.PermissionId)
                ?? throw new SaborExpress.Shared.Exceptions.ConflictException("Error al crear la asignación de permiso.");

            return RolePermissionMapper.ToResponse(created);
        }

        public async Task RemoveAsync(int roleId, int permissionId, ClaimsPrincipal actor)
        {
            EnsureCanManageGlobalPermissions(actor);
            await _rolePermissionValidator.ValidateRemoveAsync(roleId, permissionId);

            var rolePermission = await _rolePermissionRepository.GetAsync(roleId, permissionId)
                ?? throw new KeyNotFoundException("La asignación de permiso no existe.");

            await _rolePermissionRepository.RemoveAsync(rolePermission);
        }

        private static void EnsureCanManageGlobalPermissions(ClaimsPrincipal actor)
        {
            if (actor.Identity?.IsAuthenticated != true)
                throw new UnauthorizedAccessException("Debes iniciar sesi\u00f3n para modificar permisos globales.");

            if (!actor.IsInRole(RoleNames.Gerente))
                throw new ForbiddenException("Solo GERENTE puede modificar la matriz global de permisos.");
        }

        public async Task<List<RolePermissionResponseDto>> GetAllAsync()
        {
            var all = await _rolePermissionRepository.GetAllAsync();
            return all.Select(RolePermissionMapper.ToResponse).ToList();
        }

        public async Task<List<RolePermissionResponseDto>> GetByRoleIdAsync(int roleId)
        {
            var list = await _rolePermissionRepository.GetByRoleIdAsync(roleId);
            return list.Select(RolePermissionMapper.ToResponse).ToList();
        }
    }
}
