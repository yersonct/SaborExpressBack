// Modules/UserPreferences/Repositories/UserPreferenceRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.UserPreferences.Interfaces;
using SaborExpress.Modules.UserPreferences.Models;

namespace SaborExpress.Modules.UserPreferences.Repositories
{
    public class UserPreferenceRepository : IUserPreferenceRepository
    {
        private readonly AppDbContext _context;

        public UserPreferenceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserPreference?> GetByIdAsync(int id)
        {
            return await _context.UserPreferences.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<UserPreference>> GetByUserIdAsync(int userId)
        {
            return await _context.UserPreferences
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserPreference?> GetByUserScreenKeyAsync(int userId, string screen, string key)
        {
            return await _context.UserPreferences.FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Screen == screen &&
                x.Key == key);
        }

        public async Task<bool> ExistsByUserScreenKeyAsync(int userId, string screen, string key, int? excludeId = null)
        {
            return await _context.UserPreferences.AnyAsync(x =>
                x.UserId == userId &&
                x.Screen == screen &&
                x.Key == key &&
                (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        public async Task AddAsync(UserPreference preference)
        {
            await _context.UserPreferences.AddAsync(preference);
        }

        public Task UpdateAsync(UserPreference preference)
        {
            _context.UserPreferences.Update(preference);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(UserPreference preference)
        {
            _context.UserPreferences.Remove(preference);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}