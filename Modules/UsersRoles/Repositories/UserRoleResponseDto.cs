using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UserRoles.Interfaces;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Modules.UserRoles.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly AppDbContext _context;

        public UserRoleRepository(AppDbContext context) => _context = context;

        public async Task<bool> ExistsAsync(int userId, int roleId)
            => await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        public async Task AddAsync(UserRole userRole)
        {
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(UserRole userRole)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }

        public async Task<UserRole?> GetAsync(int userId, int roleId)
            => await UserRolesWithRelations()
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        public async Task<List<UserRole>> GetAllAsync()
            => await UserRolesWithRelations()
                .ToListAsync();

        public async Task<List<UserRole>> GetByUserIdAsync(int userId)
            => await UserRolesWithRelations()
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

        public async Task<bool> ExistsAdministradorInBranchAsync(int branchId, int? excludeUserId = null)
        {
            var query = _context.UserRoles
                .Include(ur => ur.Role)
                .Include(ur => ur.User)
                    .ThenInclude(u => u.Employee)
                .Where(ur => ur.Role.Name == RoleNames.Administrador
                          && ur.User.Employee != null
                          && ur.User.Employee.BranchId == branchId);

            if (excludeUserId.HasValue)
                query = query.Where(ur => ur.UserId != excludeUserId.Value);

            return await query.AnyAsync();
        }



        private IQueryable<UserRole> UserRolesWithRelations()
            => _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role);
    }
}