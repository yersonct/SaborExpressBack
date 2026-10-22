using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Notifications.Models;
using SaborExpress.Modules.UserPreferences.Constants;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Modules.Notifications.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> AddAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<(Notification Notification, bool Created)> AddIdempotentAsync(
            Notification notification)
        {
            if (string.IsNullOrWhiteSpace(notification.IdempotencyKey))
                throw new ArgumentException("La clave de idempotencia es obligatoria.", nameof(notification));

            _context.Notifications.Add(notification);
            try
            {
                await _context.SaveChangesAsync();
                return (notification, true);
            }
            catch (DbUpdateException)
            {
                _context.Entry(notification).State = EntityState.Detached;
                var existing = await _context.Notifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.IdempotencyKey == notification.IdempotencyKey);
                if (existing == null)
                    throw;

                return (existing, false);
            }
        }

        public async Task<List<Notification>> AddRangeAsync(List<Notification> notifications)
        {
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
            return notifications;
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<Notification?> GetByIdempotencyKeyAsync(string key)
        {
            return await _context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(notification => notification.IdempotencyKey == key);
        }

        public async Task<List<Notification>> GetByUserIdAsync(int userId, bool? unreadOnly = null)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);

            if (unreadOnly == true)
                query = query.Where(n => !n.IsRead);

            return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<Dictionary<int, string>> GetLanguagesByUserIdsAsync(IReadOnlyCollection<int> userIds)
        {
            var ids = userIds.Distinct().ToArray();
            if (ids.Length == 0) return new Dictionary<int, string>();

            var savedLanguages = await _context.UserPreferences
                .Where(x => ids.Contains(x.UserId)
                    && x.Screen == LanguageOptions.LanguageScreen
                    && x.Key == LanguageOptions.LanguageKey)
                .ToDictionaryAsync(x => x.UserId, x => x.Value);

            return ids.ToDictionary(
                id => id,
                id => savedLanguages.TryGetValue(id, out var language)
                    && LanguageOptions.Allowed.Contains(language, StringComparer.OrdinalIgnoreCase)
                        ? language.ToLowerInvariant()
                        : LanguageOptions.DefaultLanguage);
        }

        public async Task<List<int>> GetUserIdsByBranchIdAsync(int branchId)
        {
            // Ajusta esta consulta a como realmente relacionas Employee -> User -> Branch en tu esquema
            return await _context.Employees
                .Where(e => e.BranchId == branchId && e.Status != "Retirado")
                .Select(e => e.UserId)
                .ToListAsync();
        }

        // Destinatarios de una reseña: todos los Gerentes + los Administradores de esa sede
        public async Task<List<int>> GetReviewRecipientUserIdsAsync(int branchId)
        {
            return await _context.Users
                .Where(u => u.Status
                    && (u.Employee == null || u.Employee.Status != "Retirado")
                    && (
                        u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Gerente)
                        || (u.Employee != null
                            && u.Employee.BranchId == branchId
                            && u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Administrador))
                    ))
                .Select(u => u.Id)
                .ToListAsync();
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Notification notification)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        public async Task UpsertPushDeviceAsync(PushDevice device)
        {
            var existing = await _context.PushDevices
                .FirstOrDefaultAsync(x => x.ExpoPushToken == device.ExpoPushToken);

            if (existing == null)
            {
                device.CreatedAt = DateTime.UtcNow;
                _context.PushDevices.Add(device);
            }
            else
            {
                // Un token de una misma instalación puede cambiar de usuario al cerrar sesión.
                existing.UserId = device.UserId;
                existing.Platform = device.Platform;
                existing.NotificationProfile = device.NotificationProfile;
                existing.SoundEnabled = device.SoundEnabled;
                existing.VibrationEnabled = device.VibrationEnabled;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemovePushDeviceAsync(int userId, string expoPushToken)
        {
            var device = await _context.PushDevices.FirstOrDefaultAsync(x =>
                x.UserId == userId && x.ExpoPushToken == expoPushToken);
            if (device == null) return;

            _context.PushDevices.Remove(device);
            await _context.SaveChangesAsync();
        }

        public async Task RemovePushDevicesByUserIdAsync(int userId)
        {
            var devices = await _context.PushDevices
                .Where(x => x.UserId == userId)
                .ToListAsync();
            if (devices.Count == 0) return;

            _context.PushDevices.RemoveRange(devices);
            await _context.SaveChangesAsync();
        }

        public Task<List<PushDevice>> GetPushDevicesByUserIdAsync(int userId) =>
            _context.PushDevices.Where(x => x.UserId == userId).ToListAsync();
    }
}
