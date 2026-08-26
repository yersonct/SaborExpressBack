// Modules/Orders/Interfaces/IOrderStatusHistoryRepository.cs
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Interfaces
{
    public interface IOrderStatusHistoryRepository
    {
        Task<List<OrderStatusHistory>> GetByOrderIdAsync(int orderId);
        Task<bool> OrderExistsAsync(int orderId);
        Task AddAsync(OrderStatusHistory history);
        Task SaveChangesAsync();
    }
}