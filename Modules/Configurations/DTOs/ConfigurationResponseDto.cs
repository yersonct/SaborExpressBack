// Modules/Configurations/DTOs/ConfigurationResponseDto.cs
namespace SaborExpress.Modules.Configurations.DTOs
{
    public class ConfigurationResponseDto
    {
        public int Id { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}