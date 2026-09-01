using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.DailyMenu.Enum;
using SaborExpress.Modules.DailyMenu.Interfaces;
using SaborExpress.Modules.DailyMenu.Models;

namespace SaborExpress.Modules.DailyMenu.Repositories
{
    public class DailyMenuRepository : IDailyMenuRepository
    {
        private readonly AppDbContext _context;

        public DailyMenuRepository(AppDbContext context) => _context = context;

        private IQueryable<DailyMenuItem> WithRelations() =>
            _context.DailyMenuItems
                .Include(i => i.Branch)
                .Include(i => i.Product)
                    .ThenInclude(p => p.Category)
                .AsNoTracking();

        public async Task<List<DailyMenuItem>> GetByBranchAndDateAsync(int branchId, DateTime date, MealPeriod? period)
        {
            var query = WithRelations()
                .Where(i => i.BranchId == branchId && i.Date.Date == date.Date);

            if (period.HasValue)
                query = query.Where(i => i.MealPeriod == period.Value);

            return await query.ToListAsync();
        }

        public async Task<DailyMenuItem?> GetByIdAsync(int id)
            => await WithRelations().FirstOrDefaultAsync(i => i.Id == id);

        public async Task<bool> ExistsAsync(int branchId, int productId, DateTime date, MealPeriod period)
            => await _context.DailyMenuItems.AnyAsync(i =>
                i.BranchId == branchId &&
                i.ProductId == productId &&
                i.Date.Date == date.Date &&
                i.MealPeriod == period);

        public async Task AddAsync(DailyMenuItem item)
        {
            _context.DailyMenuItems.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<DailyMenuItem> items)
        {
            _context.DailyMenuItems.AddRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRangeAsync(int branchId, DateTime date, MealPeriod period)
        {
            var existing = await _context.DailyMenuItems
                .Where(i => i.BranchId == branchId && i.Date.Date == date.Date && i.MealPeriod == period)
                .ToListAsync();

            _context.DailyMenuItems.RemoveRange(existing);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DailyMenuItem item)
        {
            _context.DailyMenuItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(DailyMenuItem item)
        {
            _context.DailyMenuItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
