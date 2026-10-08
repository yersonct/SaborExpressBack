using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.UserPreferences.Interfaces;
using SaborExpress.Modules.UserPreferences.Models;

namespace SaborExpress.Modules.UserPreferences.Repositories
{
    public class UserSettingsRepository : IUserSettingsRepository
    {
        private readonly AppDbContext _context;

        public UserSettingsRepository(AppDbContext context) => _context = context;

        public async Task<UserSettings?> GetByUserIdAsync(int userId)
            => await _context.UserSettings.FirstOrDefaultAsync(x => x.UserId == userId);

        public async Task AddAsync(UserSettings settings)
            => await _context.UserSettings.AddAsync(settings);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}