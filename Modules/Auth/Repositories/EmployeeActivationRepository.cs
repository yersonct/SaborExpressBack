// Modules/Auth/Repositories/EmployeeActivationRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Repositories
{
    public class EmployeeActivationRepository : IEmployeeActivationRepository
    {
        private readonly AppDbContext _context;

        public EmployeeActivationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EmployeeActivationCode?> GetLatestByUserIdAsync(int userId)
        {
            return await _context.EmployeeActivationCodes
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(EmployeeActivationCode code)
        {
            await _context.EmployeeActivationCodes.AddAsync(code);
        }

        public Task UpdateAsync(EmployeeActivationCode code)
        {
            _context.EmployeeActivationCodes.Update(code);
            return Task.CompletedTask;
        }

        public async Task<EmployeeActivationCode?> GetByTokenAsync(string token)
        {
            return await _context.EmployeeActivationCodes
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Code == token);
        }

        public async Task InvalidatePendingAsync(int userId)
        {
            var pending = await _context.EmployeeActivationCodes
                .Where(x => x.UserId == userId && !x.IsUsed)
                .ToListAsync();

            foreach (var c in pending)
                c.IsUsed = true;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
