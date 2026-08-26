// Modules/Configurations/Mappings/ConfigurationMapper.cs
using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Models;

namespace SaborExpress.Modules.Configurations.Mappings
{
    public static class ConfigurationMapper
    {
        public static ConfigurationResponseDto ToResponse(BranchSetting configuration)
        {
            return new ConfigurationResponseDto
            {
                Id = configuration.Id,
                BranchId = configuration.BranchId,
                BranchName = configuration.Branch?.Name,
                Key = configuration.Key,
                Value = configuration.Value,
                DataType = configuration.DataType,
                Description = configuration.Description,
                UpdatedAt = configuration.UpdatedAt
            };
        }
    }
}