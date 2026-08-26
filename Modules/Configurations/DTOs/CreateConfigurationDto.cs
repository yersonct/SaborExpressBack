// Modules/Configurations/DTOs/CreateConfigurationDto.cs
namespace SaborExpress.Modules.Configurations.DTOs
{
    public class CreateConfigurationDto
    {
        public int? BranchId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}