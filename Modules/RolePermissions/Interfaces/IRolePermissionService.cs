using SaborExpress.Modules.RolePermissions.DTOs;

namespace SaborExpress.Modules.RolePermissions.Interfaces
{
    public interface IRolePermissionService
    {
        Task<RolePermissionResponseDto> AssignAsync(CreateRolePermissionDto dto);
        Task RemoveAsync(int roleId, int permissionId);
        Task<List<RolePermissionResponseDto>> GetAllAsync();
        Task<List<RolePermissionResponseDto>> GetByRoleIdAsync(int roleId);
    }
}