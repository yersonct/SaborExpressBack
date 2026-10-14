using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Notifications.Models
{
    public class PushDevice
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string ExpoPushToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string NotificationProfile { get; set; } = "waiter";
        public bool SoundEnabled { get; set; } = true;
        public bool VibrationEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
