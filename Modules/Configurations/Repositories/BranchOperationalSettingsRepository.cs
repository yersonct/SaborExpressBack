using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Configurations.Models;

namespace SaborExpress.Modules.Configurations.Repositories
{
    public class BranchOperationalSettingsRepository : IBranchOperationalSettingsRepository
    {
        private readonly AppDbContext _context;

        public BranchOperationalSettingsRepository(AppDbContext context) => _context = context;

        public async Task<BranchOperationalSettings?> GetByBranchIdAsync(int branchId)
            => await _context.BranchOperationalSettings.FirstOrDefaultAsync(x => x.BranchId == branchId);

        public async Task AddAsync(BranchOperationalSettings settings)
            => await _context.BranchOperationalSettings.AddAsync(settings);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}