// Modules/Deliveries/Interfaces/IDeliveryRepository.cs
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Deliveries.Interfaces
{
public interface IDeliveryRepository
{
    Task<Delivery?> GetByIdAsync(int id);
    Task<List<Delivery>> GetByOrderIdAsync(int orderId);
    Task<List<Delivery>> GetByDeliveryPersonIdAsync(int deliveryPersonId);

    // ELIMINADO: Task<List<Order>> GetAvailableOrdersAsync(int branchId);
    // Ya no hay modelo "pull" — el repartidor no elige pedidos disponibles.

    // NUEVO: candidatos para la asignación automática
    Task<List<DeliveryPersonLoad>> GetAvailableDeliveryPersonsWithLoadAsync(int branchId);

    Task<int?> GetEmployeeBranchIdAsync(int employeeId);
    Task<List<Delivery>> GetByBranchIdAsync(int branchId);

    Task<bool> OrderExistsAsync(int orderId);
    Task<bool> AddressExistsAsync(int addressId);
    Task<bool> AddressBelongsToCustomerAsync(int addressId, int customerId);
    Task<bool> OrderHasDeliveryAsync(int orderId);

    Task AddAsync(Delivery delivery);

    Task<bool> IsDeliveryPersonAsync(int employeeId);
    Task<List<DeliveryPersonSummary>> GetDeliveryPersonsByBranchAsync(int branchId);
    Task<List<int>> GetUnassignedDeliveryOrderIdsAsync(int branchId);
    Task UpdateAsync(Delivery delivery);
    Task SaveChangesAsync();
}
}