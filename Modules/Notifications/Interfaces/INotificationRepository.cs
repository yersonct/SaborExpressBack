using SaborExpress.Modules.Notifications.Models;

namespace SaborExpress.Modules.Notifications.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification> AddAsync(Notification notification);
        Task<List<Notification>> AddRangeAsync(List<Notification> notifications);
        Task<Notification?> GetByIdAsync(int id);
        Task<List<Notification>> GetByUserIdAsync(int userId, bool? unreadOnly = null);
        Task<int> GetUnreadCountAsync(int userId);
        Task<List<int>> GetUserIdsByBranchIdAsync(int branchId);
        Task<List<int>> GetReviewRecipientUserIdsAsync(int branchId);
        Task UpdateAsync(Notification notification);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteAsync(Notification notification);
    }
}