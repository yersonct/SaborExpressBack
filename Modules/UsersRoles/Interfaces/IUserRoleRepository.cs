using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UsersRoles.Models;

namespace SaborExpress.Modules.UserRoles.Interfaces
{
    public interface IUserRoleRepository
    {
        Task<bool> ExistsAsync(int userId, int roleId);
        Task AddAsync(UserRole userRole);
        Task RemoveAsync(UserRole userRole);
        Task<UserRole?> GetAsync(int userId, int roleId);
        Task<List<UserRole>> GetAllAsync();
        Task<List<UserRole>> GetByUserIdAsync(int userId);
        Task<bool> ExistsAdministradorInBranchAsync(int branchId, int? excludeUserId = null);
    }
}