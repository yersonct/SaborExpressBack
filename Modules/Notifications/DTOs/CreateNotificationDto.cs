using SaborExpress.Modules.Notifications.Enum;

namespace SaborExpress.Modules.Notifications.DTOs
{
    public class CreateNotificationDto
    {
        // Debe venir uno de los dos: destinatario puntual o toda una sede
        public int? UserId { get; set; }
        public int? BranchId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.Manual;

        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}