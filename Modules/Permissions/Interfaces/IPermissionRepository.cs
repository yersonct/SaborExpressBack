using SaborExpress.Modules.Permissions.Models;

namespace SaborExpress.Modules.Permissions.Interfaces
{
    public interface IPermissionRepository
    {
        Task<List<Permission>> GetAllAsync();
        Task<Permission?> GetByIdAsync(int id);
        Task AddAsync(Permission permission);
        Task UpdateAsync(Permission permission);
        Task<bool> ExistsByNameAsync(string name);
        Task<bool> ExistsByNameAsync(string name, int excludeId);
        Task<bool> HasAssignedRolesAsync(int permissionId);
        Task DeleteAsync(Permission permission);
    }
}