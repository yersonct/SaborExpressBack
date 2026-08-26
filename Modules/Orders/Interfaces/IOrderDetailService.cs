// Modules/Orders/Interfaces/IOrderDetailService.cs
using SaborExpress.Modules.Orders.DTOs;

namespace SaborExpress.Modules.Orders.Interfaces
{
    public interface IOrderDetailService
    {
        Task<List<OrderDetailResponseDto>> GetByOrderIdAsync(int orderId);
        Task<OrderDetailResponseDto> CreateAsync(int orderId, CreateOrderDetailDto dto, int employeeId);
        Task<OrderDetailResponseDto> UpdateAsync(int id, UpdateOrderDetailDto dto, int employeeId);
        Task<OrderDetailResponseDto> VoidAsync(int id, VoidOrderDetailDto dto, int employeeId);
    }
}