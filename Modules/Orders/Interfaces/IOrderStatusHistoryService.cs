// Modules/Orders/Interfaces/IOrderStatusHistoryService.cs
using SaborExpress.Modules.Orders.DTOs;

namespace SaborExpress.Modules.Orders.Interfaces
{
    public interface IOrderStatusHistoryService
    {
        Task<List<OrderStatusHistoryResponseDto>> GetByOrderIdAsync(int orderId);
    }
}