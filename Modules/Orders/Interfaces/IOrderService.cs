// Modules/Orders/Interfaces/IOrderService.cs
using SaborExpress.Modules.Orders.DTOs;

namespace SaborExpress.Modules.Orders.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateAsync(CreateOrderDto dto, int employeeId, bool isClienteChannel); // CAMBIADO
        Task<OrderResponseDto> GetByIdAsync(int id);
        Task<List<OrderSummaryDto>> GetAllAsync(OrderFilterDto filter);
        Task<List<OrderSummaryDto>> GetByTableIdAsync(int tableId);
        Task<List<OrderSummaryDto>> GetByCustomerIdAsync(int customerId);
        Task<List<OrderSummaryDto>> GetByBranchIdAsync(int branchId);
       Task<OrderResponseDto> UpdateAsync(int id, UpdateOrderDto dto, int employeeId);
        Task<OrderResponseDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int employeeId);
        Task<OrderResponseDto> CancelAsync(int id, CancelOrderDto dto, int employeeId);
    }
}