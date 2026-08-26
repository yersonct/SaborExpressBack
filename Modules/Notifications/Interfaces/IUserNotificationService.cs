using SaborExpress.Modules.Notifications.DTOs;
using SaborExpress.Modules.Notifications.Enum;

namespace SaborExpress.Modules.Notifications.Interfaces
{
    public interface IUserNotificationService
    {
        Task<NotificationDto> CreateAsync(
            int userId, string title, string message, NotificationType type,
            string? relatedEntityType = null, int? relatedEntityId = null);

        Task<List<NotificationDto>> CreateBulkAsync(
            List<int> userIds, string title, string message, NotificationType type,
            string? relatedEntityType = null, int? relatedEntityId = null);

        Task<List<NotificationDto>> CreateManualAsync(CreateNotificationDto dto);

        Task<List<NotificationDto>> GetByUserAsync(int userId, bool? unreadOnly = null);
        Task<int> GetUnreadCountAsync(int userId);
        Task<NotificationDto> MarkAsReadAsync(int id, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteAsync(int id, int userId);
    }
}