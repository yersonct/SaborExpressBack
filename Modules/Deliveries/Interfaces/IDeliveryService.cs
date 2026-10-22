// Modules/Deliveries/Interfaces/IDeliveryService.cs
using SaborExpress.Modules.Deliveries.DTOs;

namespace SaborExpress.Modules.Deliveries.Interfaces
{
    public interface IDeliveryService
    {
        Task<DeliveryResponseDto> CreateAsync(CreateDeliveryDto dto, int currentEmployeeId);
        Task<List<DeliveryResponseDto>> GetByOrderIdAsync(int orderId);
        Task<CustomerDeliveryTrackingDto> GetCustomerTrackingAsync(int orderId, int customerId);
        Task<List<DeliveryResponseDto>> GetByDeliveryPersonIdAsync(int deliveryPersonId, bool activeOnly = false);
        Task<List<DeliveryResponseDto>> GetByBranchIdAsync(int branchId);
        Task<DeliveryResponseDto> UpdateStatusAsync(int id, UpdateDeliveryStatusDto dto, int currentEmployeeId, bool isAdmin);
        Task<DeliveryResponseDto> RateAsync(int id, RateDeliveryDto dto, int customerId);
    }
}
