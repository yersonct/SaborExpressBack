// Modules/UserPreferences/DTOs/CreateUserPreferenceDto.cs
namespace SaborExpress.Modules.UserPreferences.DTOs
{
    // UserId sale del usuario autenticado, no del body.
    public class CreateUserPreferenceDto
    {
        public string Screen { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
    }
}