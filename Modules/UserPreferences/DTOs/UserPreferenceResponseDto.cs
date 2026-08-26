// Modules/UserPreferences/DTOs/UserPreferenceResponseDto.cs
namespace SaborExpress.Modules.UserPreferences.DTOs
{
    public class UserPreferenceResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Screen { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}