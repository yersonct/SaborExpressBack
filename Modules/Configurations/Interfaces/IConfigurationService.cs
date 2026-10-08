// Modules/Configurations/Interfaces/IConfigurationService.cs
using SaborExpress.Modules.Configurations.DTOs;

namespace SaborExpress.Modules.Configurations.Interfaces
{
    public interface IConfigurationService
    {
        Task<List<ConfigurationResponseDto>> GetAllAsync();
        Task<List<ConfigurationResponseDto>> GetByBranchIdAsync(int branchId);

        Task<int?> GetEmployeeBranchIdAsync(int employeeId);
        Task<int?> GetBranchIdByConfigurationIdAsync(int configurationId);
        Task<ConfigurationResponseDto> GetByKeyAsync(int? branchId, string key);
        Task<ConfigurationResponseDto> CreateAsync(CreateConfigurationDto dto);
        Task<ConfigurationResponseDto> UpdateAsync(int id, UpdateConfigurationDto dto);
        Task DeleteAsync(int id);
        
    }
}