using Microsoft.AspNetCore.SignalR;
using SaborExpress.Modules.Notifications.DTOs;
using SaborExpress.Modules.Notifications.Enum;
using SaborExpress.Modules.Notifications.Hubs;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Notifications.Mappings;
using SaborExpress.Modules.Notifications.Models;
using SaborExpress.Modules.UserPreferences.Constants;
using SaborExpress.Shared.Localization;

namespace SaborExpress.Modules.Notifications.Services
{
    public class UserNotificationService : IUserNotificationService  
    {
        private readonly INotificationRepository _repository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IExpoPushNotificationQueue _pushQueue;
        private readonly IApiMessageLocalizer _messageLocalizer;

        public UserNotificationService(
            INotificationRepository repository,
            IHubContext<NotificationHub> hubContext,
            IExpoPushNotificationQueue pushQueue,
            IApiMessageLocalizer messageLocalizer)
        {
            _repository = repository;
            _hubContext = hubContext;
            _pushQueue = pushQueue;
            _messageLocalizer = messageLocalizer;
        }

        public async Task<NotificationDto> CreateAsync(
            int userId, string title, string message, NotificationType type,
            string? relatedEntityType = null, int? relatedEntityId = null,
            string? idempotencyKey = null)
        {
            var entity = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : await _repository.GetByIdempotencyKeyAsync(idempotencyKey);
            if (entity == null)
            {
                entity = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    RelatedEntityType = relatedEntityType,
                    RelatedEntityId = relatedEntityId,
                    IdempotencyKey = idempotencyKey
                };

                await _repository.AddAsync(entity);
            }
            var dto = entity.ToDto();

            var language = LanguageOptions.DefaultLanguage;
            try
            {
                var languages = await _repository.GetLanguagesByUserIdsAsync(new[] { userId });
                if (languages.TryGetValue(userId, out var savedLanguage))
                    language = savedLanguage;
            }
            catch
            {
                // Language lookup must not prevent a saved notification from being delivered.
            }

            await PushAsync(userId, dto, language);
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
            Dictionary<int, string> languages;
            try
            {
                languages = await _repository.GetLanguagesByUserIdsAsync(dtos.Select(dto => dto.UserId).ToArray());
            }
            catch
            {
                // Deliver notifications in Spanish if preference lookup is unavailable.
                languages = dtos.ToDictionary(dto => dto.UserId, _ => LanguageOptions.DefaultLanguage);
            }

            await Task.WhenAll(dtos.Select(dto => PushAsync(
                dto.UserId,
                dto,
                languages.GetValueOrDefault(dto.UserId, LanguageOptions.DefaultLanguage))));

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
                await NotifyNotificationStateChangedAsync(userId);
            }

            return entity.ToDto();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await _repository.MarkAllAsReadAsync(userId);
            await NotifyNotificationStateChangedAsync(userId);
        }

        public async Task DeleteAsync(int id, int userId)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Notificación no encontrada.");

            if (entity.UserId != userId)
                throw new UnauthorizedAccessException("No puedes eliminar una notificación que no es tuya.");

            await _repository.DeleteAsync(entity);
            await NotifyNotificationStateChangedAsync(userId);
        }

        private async Task NotifyNotificationStateChangedAsync(int userId)
        {
            try
            {
                await _hubContext.Clients
                    .Group(NotificationHub.GroupName(userId))
                    .SendAsync("NotificationsChanged");
            }
            catch
            {
                // Un fallo del socket no debe impedir marcar o borrar la notificación.
            }
        }

        public Task RegisterPushDeviceAsync(int userId, PushDeviceRegistrationDto dto)
        {
            var token = dto.ExpoPushToken.Trim();
            if (!token.StartsWith("ExponentPushToken[", StringComparison.Ordinal)
                && !token.StartsWith("ExpoPushToken[", StringComparison.Ordinal))
                throw new ArgumentException("El token push de Expo no es válido.");

            return _repository.UpsertPushDeviceAsync(new PushDevice
            {
                UserId = userId,
                ExpoPushToken = token,
                Platform = dto.Platform.Trim(),
                NotificationProfile = (dto.NotificationProfile ?? "waiter").Trim().ToLowerInvariant() switch
                {
                    "cashier" => "cashier",
                    "delivery" => "delivery",
                    _ => "waiter"
                },
                SoundEnabled = dto.SoundEnabled,
                VibrationEnabled = dto.VibrationEnabled,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task UnregisterPushDeviceAsync(int userId, string expoPushToken) =>
            _repository.RemovePushDeviceAsync(userId, expoPushToken.Trim());

        private async Task PushAsync(int userId, NotificationDto dto, string language)
        {
            var localizedTitle = _messageLocalizer.Localize(dto.Title, language);
            var localizedMessage = _messageLocalizer.Localize(dto.Message, language);

            await _hubContext.Clients
                .Group(NotificationHub.GroupName(userId))
                .SendAsync("ReceiveNotification", new NotificationDto
                {
                    Id = dto.Id,
                    UserId = dto.UserId,
                    Title = localizedTitle,
                    Message = localizedMessage,
                    Type = dto.Type,
                    RelatedEntityType = dto.RelatedEntityType,
                    RelatedEntityId = dto.RelatedEntityId,
                    IsRead = dto.IsRead,
                    ReadAt = dto.ReadAt,
                    CreatedAt = dto.CreatedAt
                });

            // El push es complementario a SignalR: un problema con Expo no debe
            // hacer fallar la creaciÃ³n del pedido ni el aviso en tiempo real.
            try
            {
                _pushQueue.Enqueue(new ExpoPushMessage(
                    userId,
                    localizedTitle,
                    localizedMessage,
                    dto.Id,
                    dto.Type.ToString(),
                    dto.RelatedEntityType,
                    dto.RelatedEntityId));
            }
            catch
            {
                // No convertir una falla de push/base de dispositivos en falla del flujo existente.
            }
        }
    }
}
