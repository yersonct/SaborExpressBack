using SaborExpress.Modules.Orders.DTOs;

namespace SaborExpress.Modules.Orders.Interfaces
{
    public interface IOrderDetailService
    {
        Task<List<OrderDetailResponseDto>> GetByOrderIdAsync(int orderId);
        Task<OrderDetailResponseDto> CreateAsync(int orderId, CreateOrderDetailDto dto, int employeeId);
        Task<List<OrderDetailResponseDto>> CreateBatchAsync(int orderId, CreateOrderDetailsBatchDto dto, int? employeeId, int? customerId = null);
        Task<OrderDetailResponseDto> UpdateAsync(int id, UpdateOrderDetailDto dto, int employeeId);
        Task<OrderDetailResponseDto> VoidAsync(int id, VoidOrderDetailDto dto, int employeeId);
        Task<OrderDetailResponseDto> MarkDeliveredAsync(int id, int employeeId);
        Task<List<OrderDetailResponseDto>> UpdateBatchStatusAsync(int orderId, int batchNumber, UpdateBatchStatusDto dto, int employeeId);
    }
}