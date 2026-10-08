using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepository(AppDbContext context) => _context = context;

        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetActiveByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .Include(rt => rt.User)
                    .ThenInclude(u => u.Employee)
                .Include(rt => rt.User)
                    .ThenInclude(u => u.Customer)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);
        }

        public async Task RevokeAsync(RefreshToken token)
        {
            token.IsRevoked = true;
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllForUserAsync(int userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
                token.IsRevoked = true;

            await _context.SaveChangesAsync();
        }
    }
}
