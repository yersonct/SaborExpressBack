using Microsoft.AspNetCore.SignalR;
using SaborExpress.Modules.Notifications.DTOs;
using SaborExpress.Modules.Notifications.Enum;
using SaborExpress.Modules.Notifications.Hubs;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Notifications.Mappings;
using SaborExpress.Modules.Notifications.Models;

namespace SaborExpress.Modules.Notifications.Services
{
    public class UserNotificationService : IUserNotificationService  
    {
        private readonly INotificationRepository _repository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public UserNotificationService(INotificationRepository repository, IHubContext<NotificationHub> hubContext)
        {
            _repository = repository;
            _hubContext = hubContext;
        }

        public async Task<NotificationDto> CreateAsync(
            int userId, string title, string message, NotificationType type,
            string? relatedEntityType = null, int? relatedEntityId = null)
        {
            var entity = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            };

            await _repository.AddAsync(entity);
            var dto = entity.ToDto();

            await PushAsync(userId, dto);
            return dto;
        }

        public async Task<List<NotificationDto>> CreateBulkAsync(
            List<int> userIds, string title, string message, NotificationType type,
            string? relatedEntityType = null, int? relatedEntityId = null)
        {
            var entities = userIds.Distinct().Select(uid => new Notification
            {
                UserId = uid,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            }).ToList();

            await _repository.AddRangeAsync(entities);
            var dtos = entities.Select(e => e.ToDto()).ToList();

            foreach (var dto in dtos)
                await PushAsync(dto.UserId, dto);

            return dtos;
        }

        public async Task<List<NotificationDto>> CreateManualAsync(CreateNotificationDto dto)
        {
            if (dto.UserId == null && dto.BranchId == null)
                throw new ArgumentException("Debes especificar UserId o BranchId como destinatario.");

            if (dto.UserId != null)
            {
                var result = await CreateAsync(dto.UserId.Value, dto.Title, dto.Message, dto.Type,
                    dto.RelatedEntityType, dto.RelatedEntityId);
                return new List<NotificationDto> { result };
            }

            var userIds = await _repository.GetUserIdsByBranchIdAsync(dto.BranchId!.Value);
            if (userIds.Count == 0)
                throw new KeyNotFoundException("No hay empleados activos en esa sucursal.");

            return await CreateBulkAsync(userIds, dto.Title, dto.Message, dto.Type,
                dto.RelatedEntityType, dto.RelatedEntityId);
        }

        public async Task<List<NotificationDto>> GetByUserAsync(int userId, bool? unreadOnly = null)
        {
            var list = await _repository.GetByUserIdAsync(userId, unreadOnly);
            return list.Select(n => n.ToDto()).ToList();
        }

        public Task<int> GetUnreadCountAsync(int userId) => _repository.GetUnreadCountAsync(userId);

        public async Task<NotificationDto> MarkAsReadAsync(int id, int userId)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Notificación no encontrada.");

            if (entity.UserId != userId)
                throw new UnauthorizedAccessException("No puedes marcar como leída una notificación que no es tuya.");

            if (!entity.IsRead)
            {
                entity.IsRead = true;
                entity.ReadAt = DateTime.UtcNow;
                await _repository.UpdateAsync(entity);
            }

            return entity.ToDto();
        }

        public Task MarkAllAsReadAsync(int userId) => _repository.MarkAllAsReadAsync(userId);

        public async Task DeleteAsync(int id, int userId)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Notificación no encontrada.");

            if (entity.UserId != userId)
                throw new UnauthorizedAccessException("No puedes eliminar una notificación que no es tuya.");

            await _repository.DeleteAsync(entity);
        }

        private async Task PushAsync(int userId, NotificationDto dto)
        {
            await _hubContext.Clients
                .Group(NotificationHub.GroupName(userId))
                .SendAsync("ReceiveNotification", dto);
        }
    }
}