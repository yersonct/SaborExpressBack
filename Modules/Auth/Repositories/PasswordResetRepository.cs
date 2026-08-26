using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Repositories
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly AppDbContext _context;

        public PasswordResetRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PasswordResetCode resetCode)
        {
            await _context.PasswordResetCodes.AddAsync(resetCode);
            await _context.SaveChangesAsync();
        }

        public async Task<PasswordResetCode?> GetActiveCodeAsync(int userId, string code)
        {
            return await _context.PasswordResetCodes
                .Where(r => r.UserId == userId
                    && r.Code == code
                    && !r.IsUsed
                    && r.CodeExpiresAt > DateTime.UtcNow)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<PasswordResetCode?> GetByResetTokenAsync(string resetToken)
        {
            return await _context.PasswordResetCodes
                .Include(r => r.User)
                .Where(r => r.ResetToken == resetToken
                    && !r.IsCompleted
                    && r.ResetTokenExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        public async Task InvalidatePendingCodesAsync(int userId)
        {
            var pendingCodes = await _context.PasswordResetCodes
                .Where(c => c.UserId == userId && !c.IsUsed && !c.IsCompleted)
                .ToListAsync();

            foreach (var code in pendingCodes)
                code.IsUsed = true;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PasswordResetCode resetCode)
        {
            _context.PasswordResetCodes.Update(resetCode);
            await _context.SaveChangesAsync();
        }

        public async Task<List<DateTime>> GetRecentAttemptTimestampsAsync(int userId, DateTime since)
        {
            return await _context.PasswordResetCodes
                .Where(r => r.UserId == userId && r.CreatedAt >= since)
                .OrderBy(r => r.CreatedAt)
                .Select(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<PasswordResetCode?> GetActiveCodeByUserIdAsync(int userId)
        {
            return await _context.PasswordResetCodes
                .Where(r => r.UserId == userId
                    && !r.IsUsed
                    && r.CodeExpiresAt > DateTime.UtcNow)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<PasswordResetCode?> GetLatestCodeByUserIdAsync(int userId)
        {
            return await _context.PasswordResetCodes
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
