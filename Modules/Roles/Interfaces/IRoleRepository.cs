using SaborExpress.Modules.Roles.Models;

namespace SaborExpress.Modules.Roles.Interfaces
{
    public interface IRoleRepository
    {
        Task<List<Role>> GetAllAsync();
        Task<Role?> GetByIdAsync(int id);
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);

        Task<bool> ExistsByNameAsync(string name);
        Task<Role?> GetByNameAsync(string name);
        Task<bool> ExistsByNameAsync(string name, int excludeId);
        Task<bool> HasAssignedUsersAsync(int roleId);
        Task DeleteAsync(Role role);

        // Trae los roles completos para validar existencia Y revisar RequiresCv,
        // todo en 1 sola consulta (reemplaza al viejo CountByIdsAsync)
        Task<List<Role>> GetByIdsAsync(List<int> ids);
    }
}