using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.UserPreferences.Models
{
    // Una fila por usuario. El idioma NO va aquí: sigue en UserPreference (Global/Language).
    public class UserSettings
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Theme { get; set; } = "system"; // "light" | "dark" | "system"
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool SoundNotifications { get; set; } = true;
        public string TimeZone { get; set; } = "America/Bogota";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}