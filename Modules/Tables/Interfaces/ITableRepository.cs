// Modules/Tables/Interfaces/ITableRepository.cs
using SaborExpress.Modules.Tables.Models;

namespace SaborExpress.Modules.Tables.Interfaces
{
    public interface ITableRepository
    {
        Task<Table?> GetByIdAsync(int id);
        Task<List<Table>> GetByBranchIdAsync(int branchId);
        Task<bool> BranchExistsAsync(int branchId);
        Task<bool> ExistsByNumberInBranchAsync(int branchId, int number, int? excludeId = null);
        Task AddAsync(Table table);
        Task UpdateAsync(Table table);
        Task DeleteAsync(Table table);
        Task SaveChangesAsync();
    }
}