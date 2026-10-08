// Modules/Tables/Repositories/TableRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Modules.Tables.Models;
using SaborExpress.Modules.Tables.Enum;


namespace SaborExpress.Modules.Tables.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly AppDbContext _context;

        public TableRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Table?> GetByIdAsync(int id)
        {
            return await _context.Tables
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Table>> GetByBranchIdAsync(int branchId)
        {
            return await _context.Tables
                .Include(x => x.Branch)
                .Where(x => x.BranchId == branchId)
                .OrderBy(x => x.Number)
                .ToListAsync();
        }

        public async Task<bool> BranchExistsAsync(int branchId)
        {
            return await _context.Branches.AnyAsync(x => x.Id == branchId);
        }

        public async Task<bool> ExistsByNumberInBranchAsync(int branchId, int number, int? excludeId = null)
        {
            return await _context.Tables.AnyAsync(x =>
                x.BranchId == branchId &&
                x.Number == number &&
                !x.IsDeleted &&
                (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        public async Task AddAsync(Table table)
        {
            await _context.Tables.AddAsync(table);
        }

        public Task UpdateAsync(Table table)
        {
            _context.Tables.Update(table);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Table table)
        {
            _context.Tables.Remove(table);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}