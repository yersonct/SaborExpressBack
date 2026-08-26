// Modules/UserPreferences/Interfaces/IUserPreferenceRepository.cs
using SaborExpress.Modules.UserPreferences.Models;

namespace SaborExpress.Modules.UserPreferences.Interfaces
{
    public interface IUserPreferenceRepository
    {
        Task<UserPreference?> GetByIdAsync(int id);
        Task<List<UserPreference>> GetByUserIdAsync(int userId);
        Task<UserPreference?> GetByUserScreenKeyAsync(int userId, string screen, string key);
        Task<bool> ExistsByUserScreenKeyAsync(int userId, string screen, string key, int? excludeId = null);

        Task AddAsync(UserPreference preference);
        Task UpdateAsync(UserPreference preference);
        Task DeleteAsync(UserPreference preference);
        Task SaveChangesAsync();
    }
}