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

        // NUEVO: pedidos Delivery + Ready/Confirmed que aún no tienen repartidor asignado
        Task<List<Order>> GetAvailableOrdersAsync();

        // NUEVO: para restringir Administrador a su propia sede
        Task<int?> GetEmployeeBranchIdAsync(int employeeId);

        // NUEVO: entregas de todos los repartidores de una sede (para Administrador/Gerente)
        Task<List<Delivery>> GetByBranchIdAsync(int branchId);

        Task<bool> OrderExistsAsync(int orderId);
        Task<bool> AddressExistsAsync(int addressId);
        Task<bool> AddressBelongsToCustomerAsync(int addressId, int customerId);
        Task<bool> OrderHasDeliveryAsync(int orderId);
        Task<bool> EmployeeHasDeliveryRoleAsync(int employeeId);

        Task AddAsync(Delivery delivery);
        Task UpdateAsync(Delivery delivery);
        Task SaveChangesAsync();
    }
}