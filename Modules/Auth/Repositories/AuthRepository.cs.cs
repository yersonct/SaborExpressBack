using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using System.Threading.Tasks;

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
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsByDocumentAsync(string document)
        {
            return await _context.Users.AnyAsync(u => u.Document == document);
        }

        public async Task<User?> GetByIdentifierAsync(string identifier)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == identifier || u.Document == identifier);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}