// Modules/Orders/Interfaces/IOrderDetailRepository.cs
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Interfaces
{
    public interface IOrderDetailRepository
    {
        Task<OrderDetail?> GetByIdAsync(int id);
        Task<List<OrderDetail>> GetByOrderIdAsync(int orderId);
        Task<bool> OrderExistsAsync(int orderId);
        Task AddAsync(OrderDetail orderDetail);
        Task UpdateAsync(OrderDetail orderDetail);
        Task SaveChangesAsync();
    }
}