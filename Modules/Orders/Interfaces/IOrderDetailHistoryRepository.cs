// Modules/Orders/Interfaces/IOrderDetailHistoryRepository.cs
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Interfaces
{
    public interface IOrderDetailHistoryRepository
    {
        Task<List<OrderDetailHistory>> GetByOrderDetailIdAsync(int orderDetailId);
        Task<bool> OrderDetailExistsAsync(int orderDetailId);
        Task AddAsync(OrderDetailHistory history);
        Task SaveChangesAsync();
    }
}