using SaborExpress.Modules.UserPreferences.Models;

namespace SaborExpress.Modules.UserPreferences.Interfaces
{
    public interface IUserSettingsRepository
    {
        Task<UserSettings?> GetByUserIdAsync(int userId);
        Task AddAsync(UserSettings settings);
        Task SaveChangesAsync();
    }
}