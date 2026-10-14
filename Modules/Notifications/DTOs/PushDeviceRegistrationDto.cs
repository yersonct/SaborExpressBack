using System.ComponentModel.DataAnnotations;

namespace SaborExpress.Modules.Notifications.DTOs
{
    public class PushDeviceRegistrationDto
    {
        [Required]
        [MaxLength(256)]
        public string ExpoPushToken { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Platform { get; set; } = string.Empty;

        [MaxLength(20)]
        public string NotificationProfile { get; set; } = "waiter";

        public bool SoundEnabled { get; set; } = true;
        public bool VibrationEnabled { get; set; } = true;
    }
}
