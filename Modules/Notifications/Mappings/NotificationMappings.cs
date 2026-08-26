using SaborExpress.Modules.Notifications.DTOs;
using SaborExpress.Modules.Notifications.Models;

namespace SaborExpress.Modules.Notifications.Mappings
{
    public static class NotificationMappings
    {
        public static NotificationDto ToDto(this Notification entity)
        {
            return new NotificationDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Title = entity.Title,
                Message = entity.Message,
                Type = entity.Type,
                RelatedEntityType = entity.RelatedEntityType,
                RelatedEntityId = entity.RelatedEntityId,
                IsRead = entity.IsRead,
                ReadAt = entity.ReadAt,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}