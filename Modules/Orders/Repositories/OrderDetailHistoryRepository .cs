// Modules/Orders/Repositories/OrderDetailHistoryRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Repositories
{
    public class OrderDetailHistoryRepository : IOrderDetailHistoryRepository
    {
        private readonly AppDbContext _context;

        public OrderDetailHistoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderDetailHistory>> GetByOrderDetailIdAsync(int orderDetailId)
        {
            return await _context.OrderDetailHistories
                .Include(x => x.ChangedByEmployee)
                .Where(x => x.OrderDetailId == orderDetailId)
                .OrderByDescending(x => x.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> OrderDetailExistsAsync(int orderDetailId)
        {
            return await _context.OrderDetails.AnyAsync(x => x.Id == orderDetailId);
        }

        public async Task AddAsync(OrderDetailHistory history)
        {
            await _context.OrderDetailHistories.AddAsync(history);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}