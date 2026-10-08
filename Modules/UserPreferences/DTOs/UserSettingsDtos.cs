namespace SaborExpress.Modules.UserPreferences.DTOs
{
    public class UserSettingsResponseDto
    {
        public string Theme { get; set; } = "system";
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool SoundNotifications { get; set; }
        public string TimeZone { get; set; } = "America/Bogota";
        public DateTime? UpdatedAt { get; set; }
        public bool IsDefault { get; set; }
    }

    public class UpdateUserSettingsDto
    {
        public string Theme { get; set; } = "system";
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool SoundNotifications { get; set; }
        public string TimeZone { get; set; } = "America/Bogota";
    }
}