using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            var normalizado = NormalizarEmail(email);
            return await _context.Users.AnyAsync(u => u.Email!.ToLower() == normalizado);
        }

        public async Task<User?> GetByIdentifierAsync(string email)
        {
            var normalizado = NormalizarEmail(email);

            return await IncluirRoles(_context.Users)
                .Include(u => u.Employee)
                .Include(u => u.Customer) // NUEVO
                .FirstOrDefaultAsync(u => u.Email!.ToLower() == normalizado);
        }

        public async Task<User?> GetByIdWithRelationsAsync(int id)
        {
            var query = IncluirRoles(_context.Users)
                .Include(u => u.Employee)
                .Include(u => u.Customer);

            return await query.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        private static IQueryable<User> IncluirRoles(IQueryable<User> query) =>
            query.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

        private static string NormalizarEmail(string email) =>
            email.Trim().ToLower();

        public async Task<bool> ExistsByEmailAsync(string email, int excludeUserId)
        {
            var normalizado = NormalizarEmail(email);
            return await _context.Users.AnyAsync(u => u.Email!.ToLower() == normalizado && u.Id != excludeUserId);
        }

        public async Task<int?> GetUserIdByEmployeeIdAsync(int employeeId)
        {
            return await _context.Users
                .Where(u => u.Employee != null && u.Employee.Id == employeeId)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();
        }
    }
}
