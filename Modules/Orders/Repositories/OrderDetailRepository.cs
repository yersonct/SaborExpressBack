// Modules/Orders/Repositories/OrderDetailRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Repositories
{
    public class OrderDetailRepository : IOrderDetailRepository
    {
        private readonly AppDbContext _context;

        public OrderDetailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDetail?> GetByIdAsync(int id)
        {
            return await _context.OrderDetails
                .Include(x => x.Product)
                .Include(x => x.LastModifiedByEmployee)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<OrderDetail>> GetByOrderIdAsync(int orderId)
        {
            return await _context.OrderDetails
                .Include(x => x.Product)
                .Include(x => x.LastModifiedByEmployee)
                .Where(x => x.OrderId == orderId)
                .ToListAsync();
        }
        public async Task<List<OrderDetail>> GetByOrderAndBatchAsync(int orderId, int batchNumber)
        {
            return await _context.OrderDetails
                .Include(x => x.Product)
                .Include(x => x.LastModifiedByEmployee)
                .Where(x => x.OrderId == orderId && x.BatchNumber == batchNumber)
                .ToListAsync();
        }

        public async Task<int> GetNextBatchNumberAsync(int orderId)
        {
            var maxBatch = await _context.OrderDetails
                .Where(x => x.OrderId == orderId)
                .Select(x => (int?)x.BatchNumber)
                .MaxAsync();

            return (maxBatch ?? 0) + 1;
        }
        public async Task<bool> OrderExistsAsync(int orderId)
        {
            return await _context.Orders.AnyAsync(x => x.Id == orderId);
        }

        public async Task AddAsync(OrderDetail orderDetail)
        {
            await _context.OrderDetails.AddAsync(orderDetail);
        }

        public Task UpdateAsync(OrderDetail orderDetail)
        {
            _context.OrderDetails.Update(orderDetail);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}