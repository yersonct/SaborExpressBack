// Modules/Orders/Repositories/OrderStatusHistoryRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Repositories
{
    public class OrderStatusHistoryRepository : IOrderStatusHistoryRepository
    {
        private readonly AppDbContext _context;

        public OrderStatusHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderStatusHistory>> GetByOrderIdAsync(int orderId)
        {
            return await _context.OrderStatusHistories
                .Include(x => x.ChangedByEmployee)
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> OrderExistsAsync(int orderId)
        {
            return await _context.Orders.AnyAsync(x => x.Id == orderId);
        }

        public async Task AddAsync(OrderStatusHistory history)
        {
            await _context.OrderStatusHistories.AddAsync(history);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}