using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Roles.Interfaces;
using SaborExpress.Modules.Roles.Models;

namespace SaborExpress.Modules.Roles.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetAllAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task AddAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Roles
                .AnyAsync(r => r.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> ExistsByNameAsync(string name, int excludeId)
        {
            return await _context.Roles
                .AnyAsync(r => r.Name.ToLower() == name.ToLower() && r.Id != excludeId);
        }

        public async Task<bool> HasAssignedUsersAsync(int roleId)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.RoleId == roleId);
        }

        public async Task DeleteAsync(Role role)
        {
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }

        public async Task<Role?> GetByNameAsync(string name)
            => await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);

        public async Task<List<Role>> GetByIdsAsync(List<int> ids)
        {
            return await _context.Roles
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();
        }
    }
}