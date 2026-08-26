using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Notifications.Enum;

namespace SaborExpress.Modules.Notifications.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        // Referencia polimórfica: a qué entidad apunta esta notificación
        public string? RelatedEntityType { get; set; } // "Order", "Delivery", "Payment"...
        public int? RelatedEntityId { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}