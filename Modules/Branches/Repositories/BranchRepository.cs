using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.Branches.Models;

namespace SaborExpress.Modules.Branches.Repositories
{
    public class BranchRepository : IBranchRepository
    {
        private readonly AppDbContext _context;

        public BranchRepository(AppDbContext context) => _context = context;

        public async Task<bool> ExistsByNameAsync(string name)
            => await _context.Branches.AnyAsync(b => b.Name.ToLower() == name.ToLower());

        public async Task<bool> ExistsByNameAsync(string name, int excludeId)
            => await _context.Branches.AnyAsync(b => b.Name.ToLower() == name.ToLower() && b.Id != excludeId);

        public async Task<bool> HasEmployeesAsync(int branchId)
            => await _context.Employees.AnyAsync(e => e.BranchId == branchId);

        public async Task AddAsync(Branch branch)
        {
            await _context.Branches.AddAsync(branch);
            await _context.SaveChangesAsync();
        }

        public async Task<Branch?> GetByIdAsync(int id)
            => await _context.Branches.FirstOrDefaultAsync(b => b.Id == id);

        public async Task<Branch?> GetByPublicKitchenCodeAsync(string code)
            => await _context.Branches.FirstOrDefaultAsync(b => b.PublicKitchenCode == code);

        public async Task UpdateAsync(Branch branch)
        {
            _context.Branches.Update(branch);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasTablesAsync(int branchId)
            => await _context.Tables.AnyAsync(t => t.BranchId == branchId);

        public async Task<bool> HasOrdersAsync(int branchId)
            => await _context.Orders.AnyAsync(o => o.BranchId == branchId);

        public async Task<bool> HasSchedulesAsync(int branchId)
            => await _context.EmployeeSchedules.AnyAsync(s => s.BranchId == branchId);
        public async Task<List<Branch>> GetAllActiveWithCoordinatesAsync()
        => await _context.Branches
            .Where(b => b.Status)
            .ToListAsync();
        public async Task DeleteAsync(Branch branch)
        {
            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();
        }

        public async Task<List<BranchSummary>> GetAllSummariesAsync()
            => await _context.Branches
                .Select(b => new BranchSummary
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    Phone = b.Phone,
                    Status = b.Status,
                    EmployeeCount = b.Employees.Count(),
                    Latitude = b.Latitude,
                    Longitude = b.Longitude
                })
                .ToListAsync();

        public async Task<BranchSummary?> GetSummaryByIdAsync(int id)
            => await _context.Branches
                .Where(b => b.Id == id)
                .Select(b => new BranchSummary
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    Phone = b.Phone,
                    Status = b.Status,
                    EmployeeCount = b.Employees.Count(),
                    Latitude = b.Latitude,
                    Longitude = b.Longitude
                })
                .FirstOrDefaultAsync();

        public async Task<int> GetEmployeeCountAsync(int branchId)
            => await _context.Employees.CountAsync(e => e.BranchId == branchId);
    }
}