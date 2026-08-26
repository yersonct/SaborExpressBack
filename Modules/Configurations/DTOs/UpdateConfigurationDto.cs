// Modules/Configurations/DTOs/UpdateConfigurationDto.cs
namespace SaborExpress.Modules.Configurations.DTOs
{
    public class UpdateConfigurationDto
    {
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}