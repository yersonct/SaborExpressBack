using System.Security.Claims;
using SaborExpress.Modules.RolePermissions.DTOs;

namespace SaborExpress.Modules.RolePermissions.Interfaces
{
    public interface IRolePermissionService
    {
        Task<RolePermissionResponseDto> AssignAsync(CreateRolePermissionDto dto, ClaimsPrincipal actor);
        Task RemoveAsync(int roleId, int permissionId, ClaimsPrincipal actor);
        Task<List<RolePermissionResponseDto>> GetAllAsync();
        Task<List<RolePermissionResponseDto>> GetByRoleIdAsync(int roleId);
    }
}
