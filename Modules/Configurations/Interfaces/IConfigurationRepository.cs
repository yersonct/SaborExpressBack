// Modules/Configurations/Interfaces/IConfigurationRepository.cs
using SaborExpress.Modules.Configurations.Models;

namespace SaborExpress.Modules.Configurations.Interfaces
{
    public interface IConfigurationRepository
    {
        Task<BranchSetting?> GetByIdAsync(int id);
        Task<List<BranchSetting>> GetAllAsync();
        Task<List<BranchSetting>> GetByBranchIdAsync(int branchId);
        Task<BranchSetting?> GetByKeyAsync(int? branchId, string key);
        Task<bool> BranchExistsAsync(int branchId);
        Task<int?> GetEmployeeBranchIdAsync(int employeeId);
        Task<int?> GetBranchIdByConfigurationIdAsync(int configurationId);
        Task<bool> ExistsByBranchAndKeyAsync(int? branchId, string key, int? excludeId = null);

        Task AddAsync(BranchSetting configuration);
        Task UpdateAsync(BranchSetting configuration);
        Task DeleteAsync(BranchSetting configuration);
        Task SaveChangesAsync();
    }
}