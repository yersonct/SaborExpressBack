// Modules/Deliveries/Interfaces/IDeliveryService.cs
using SaborExpress.Modules.Deliveries.DTOs;

namespace SaborExpress.Modules.Deliveries.Interfaces
{
    public interface IDeliveryService
    {
        Task<DeliveryResponseDto> CreateAsync(CreateDeliveryDto dto, int currentEmployeeId);

        // Ahora recibe currentEmployeeId para resolver su sede internamente
        Task<List<AvailableOrderDto>> GetAvailableOrdersAsync(int currentEmployeeId);

        Task<List<DeliveryResponseDto>> GetByOrderIdAsync(int orderId);
        Task<List<DeliveryResponseDto>> GetByDeliveryPersonIdAsync(int deliveryPersonId);
        Task<List<DeliveryResponseDto>> GetByBranchIdAsync(int branchId);

        Task<DeliveryResponseDto> UpdateStatusAsync(
            int id,
            UpdateDeliveryStatusDto dto,
            int currentEmployeeId,
            bool isAdmin);
    }
}