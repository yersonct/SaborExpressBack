using SaborExpress.Modules.Configurations.DTOs;

namespace SaborExpress.Modules.Configurations.Interfaces
{
    public interface IBranchOperationalSettingsService
    {
        Task<BranchOperationalSettingsResponseDto> GetAsync(int branchId);
        Task<BranchOperationalSettingsResponseDto> UpdateAsync(int branchId, UpdateBranchOperationalSettingsDto dto, int currentUserId);
    }
}