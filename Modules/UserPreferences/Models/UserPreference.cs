// Modules/UserPreferences/Models/UserPreference.cs
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.UserPreferences.Models
{
    public class UserPreference
    {
        public int Id { get; set; }

        public int UserId { get; set; } // FK
        public User User { get; set; } = null!;

        public string Screen { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty; // "string", "bool", "int", "decimal"
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}