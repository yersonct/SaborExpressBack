// Modules/Configurations/Repositories/ConfigurationRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Configurations.Models;

namespace SaborExpress.Modules.Configurations.Repositories
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly AppDbContext _context;

        public ConfigurationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BranchSetting?> GetByIdAsync(int id)
        {
            return await _context.Configurations
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<BranchSetting>> GetAllAsync()
        {
            return await _context.Configurations
                .Include(x => x.Branch)
                .ToListAsync();
        }

        public async Task<List<BranchSetting>> GetByBranchIdAsync(int branchId)
        {
            return await _context.Configurations
                .Include(x => x.Branch)
                .Where(x => x.BranchId == branchId || x.BranchId == null)
                .ToListAsync();
        }

        public async Task<BranchSetting?> GetByKeyAsync(int? branchId, string key)
        {
            return await _context.Configurations
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x => x.BranchId == branchId && x.Key == key);
        }

        public async Task<bool> BranchExistsAsync(int branchId)
        {
            return await _context.Branches.AnyAsync(x => x.Id == branchId);
        }

        public async Task<bool> ExistsByBranchAndKeyAsync(int? branchId, string key, int? excludeId = null)
        {
            return await _context.Configurations.AnyAsync(x =>
                x.BranchId == branchId &&
                x.Key == key &&
                (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        public async Task AddAsync(BranchSetting configuration)
        {
            await _context.Configurations.AddAsync(configuration);
        }

        public Task UpdateAsync(BranchSetting configuration)
        {
            _context.Configurations.Update(configuration);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(BranchSetting configuration)
        {
            _context.Configurations.Remove(configuration);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}