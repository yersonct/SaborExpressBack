// Modules/Reviews/Repositories/ReviewRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Reviews.Interfaces;
using SaborExpress.Modules.Reviews.Models;

namespace SaborExpress.Modules.Reviews.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _context;

        public ReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        // Liviana: solo lo que ReviewMapper realmente usa (Customer),
        // sin cargar el Order completo. AsNoTracking porque son solo lecturas.
        private IQueryable<Review> BaseQuery()
        {
            return _context.Reviews
                .Include(x => x.Customer)
                .AsNoTracking();
        }

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        // Usada solo cuando de verdad hace falta saber la sede del pedido
        // (ej. DeleteAsync, para validar el permiso del Administrador).
        public async Task<Review?> GetByIdWithOrderAsync(int id)
        {
            return await _context.Reviews
                .Include(x => x.Customer)
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Review?> GetByOrderIdAsync(int orderId)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.OrderId == orderId);
        }

        public async Task<List<Review>> GetByCustomerIdAsync(int customerId)
        {
            return await BaseQuery()
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Review>> GetByBranchIdAsync(int branchId)
        {
            // El filtro por Order.BranchId genera un JOIN en SQL solo para
            // filtrar — no requiere el Include para cargar el objeto completo.
            return await BaseQuery()
                .Where(x => x.Order.BranchId == branchId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<(double Average, int Count)> GetBranchSummaryAsync(int branchId)
        {
            var query = _context.Reviews.Where(x => x.Order.BranchId == branchId);

            var count = await query.CountAsync();
            if (count == 0)
                return (0, 0);

            var average = await query.AverageAsync(x => x.Rating);
            return (average, count);
        }

        public async Task<int?> GetEmployeeBranchIdAsync(int employeeId)
        {
            return await _context.Employees
                .Where(e => e.Id == employeeId)
                .Select(e => (int?)e.BranchId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> OrderExistsAndIsReviewableAsync(int orderId)
        {
            return await _context.Orders
                .AnyAsync(x => x.Id == orderId
                    && x.Status == OrderStatus.Delivered
                    && x.Channel == OrderChannel.App);
        }

        public async Task<bool> OrderHasReviewAsync(int orderId)
        {
            return await _context.Reviews.AnyAsync(x => x.OrderId == orderId);
        }

        public async Task<bool> OrderBelongsToCustomerAsync(int orderId, int customerId)
        {
            return await _context.Orders.AnyAsync(x => x.Id == orderId && x.CustomerId == customerId);
        }

        public async Task AddAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);
        }

        public Task UpdateAsync(Review review)
        {
            _context.Reviews.Update(review);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Review review)
        {
            _context.Reviews.Remove(review);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}