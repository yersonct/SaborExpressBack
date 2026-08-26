using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.RolePermissions.Interfaces;
using SaborExpress.Modules.RolePermissions.Models;

namespace SaborExpress.Modules.RolePermissions.Repositories
{
    public class RolePermissionRepository : IRolePermissionRepository
    {
        private readonly AppDbContext _context;

        public RolePermissionRepository(AppDbContext context) => _context = context;

        public async Task<bool> ExistsAsync(int roleId, int permissionId)
            => await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        public async Task AddAsync(RolePermission rolePermission)
        {
            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(RolePermission rolePermission)
        {
            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();
        }

        public async Task<RolePermission?> GetAsync(int roleId, int permissionId)
            => await RolePermissionsWithRelations()
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        public async Task<List<RolePermission>> GetAllAsync()
            => await RolePermissionsWithRelations()
                .ToListAsync();

        public async Task<List<RolePermission>> GetByRoleIdAsync(int roleId)
            => await RolePermissionsWithRelations()
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

      
        private IQueryable<RolePermission> RolePermissionsWithRelations()
            => _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission);
    }
}