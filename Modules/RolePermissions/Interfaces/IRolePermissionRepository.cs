using SaborExpress.Modules.RolePermissions.Models;

namespace SaborExpress.Modules.RolePermissions.Interfaces
{
    public interface IRolePermissionRepository
    {
        Task<bool> ExistsAsync(int roleId, int permissionId);
        Task AddAsync(RolePermission rolePermission);
        Task RemoveAsync(RolePermission rolePermission);
        Task<RolePermission?> GetAsync(int roleId, int permissionId);
        Task<List<RolePermission>> GetAllAsync();
        Task<List<RolePermission>> GetByRoleIdAsync(int roleId);
    }
}