using System.ComponentModel.DataAnnotations;

namespace SaborExpress.Modules.Notifications.DTOs
{
    public class UnregisterPushDeviceDto
    {
        [Required]
        [MaxLength(256)]
        public string ExpoPushToken { get; set; } = string.Empty;
    }
}
