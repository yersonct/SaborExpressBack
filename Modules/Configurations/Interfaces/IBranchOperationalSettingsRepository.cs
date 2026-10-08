using SaborExpress.Modules.Configurations.Models;

namespace SaborExpress.Modules.Configurations.Interfaces
{
    public interface IBranchOperationalSettingsRepository
    {
        Task<BranchOperationalSettings?> GetByBranchIdAsync(int branchId);
        Task AddAsync(BranchOperationalSettings settings);
        Task SaveChangesAsync();
    }
}