using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.Repositories
{
    public class EmailConfirmationRepository : IEmailConfirmationRepository
    {
        private readonly AppDbContext _context;

        public EmailConfirmationRepository(AppDbContext context) => _context = context;

        public async Task AddAsync(EmailConfirmationCode code)
        {
            await _context.EmailConfirmationCodes.AddAsync(code);
            await _context.SaveChangesAsync();
        }

        public async Task<EmailConfirmationCode?> GetActiveCodeByUserIdAsync(int userId)
        {
            return await _context.EmailConfirmationCodes
                .Where(c => c.UserId == userId && !c.IsUsed && c.CodeExpiresAt > DateTime.UtcNow)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task InvalidatePendingCodesAsync(int userId)
        {
            var pending = await _context.EmailConfirmationCodes
                .Where(c => c.UserId == userId && !c.IsUsed)
                .ToListAsync();

            foreach (var code in pending)
                code.IsUsed = true;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EmailConfirmationCode code)
        {
            _context.EmailConfirmationCodes.Update(code);
            await _context.SaveChangesAsync();
        }

        public async Task<EmailConfirmationCode?> GetLatestCodeByUserIdAsync(int userId)
        {
            return await _context.EmailConfirmationCodes
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
