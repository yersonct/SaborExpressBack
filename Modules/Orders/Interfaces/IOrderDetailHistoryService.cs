// Modules/Orders/Interfaces/IOrderDetailHistoryService.cs
using SaborExpress.Modules.Orders.DTOs;

namespace SaborExpress.Modules.Orders.Interfaces
{
    public interface IOrderDetailHistoryService
    {
        Task<List<OrderDetailHistoryResponseDto>> GetByOrderDetailIdAsync(int orderDetailId);
    }
}