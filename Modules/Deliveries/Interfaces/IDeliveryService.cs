// Modules/Deliveries/Interfaces/IDeliveryService.cs
using SaborExpress.Modules.Deliveries.DTOs;

namespace SaborExpress.Modules.Deliveries.Interfaces
{
    public interface IDeliveryService
    {
        // CAMBIADO: recibe currentEmployeeId (quien toma el pedido)
        Task<DeliveryResponseDto> CreateAsync(CreateDeliveryDto dto, int currentEmployeeId);

        // NUEVO
        Task<List<AvailableOrderDto>> GetAvailableOrdersAsync();

        Task<List<DeliveryResponseDto>> GetByOrderIdAsync(int orderId);
        Task<List<DeliveryResponseDto>> GetByDeliveryPersonIdAsync(int deliveryPersonId);

        // NUEVO
        Task<List<DeliveryResponseDto>> GetByBranchIdAsync(int branchId);

        Task<DeliveryResponseDto> UpdateStatusAsync(
            int id,
            UpdateDeliveryStatusDto dto,
            int currentEmployeeId,
            bool isAdmin);
    }
}