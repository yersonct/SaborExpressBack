using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Orders.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int id);
        Task<List<Order>> GetAllAsync(int? branchId, OrderStatus? status);
        Task<List<Order>> GetByTableIdAsync(int tableId);
        Task<List<Order>> GetByCustomerIdAsync(int customerId);
        Task<List<Order>> GetByBranchIdAsync(int branchId);

        Task<bool> CustomerExistsAsync(int customerId);
        Task<bool> EmployeeExistsAsync(int employeeId);
        Task<bool> TableExistsInBranchAsync(int tableId, int branchId);
        Task<bool> BranchExistsAsync(int branchId);

        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task SaveChangesAsync();
        Task RecalculateTotalsAsync(int orderId, decimal taxRate);

        Task<OrderStatus?> GetOrderStatusAsync(int orderId);

        // Nuevo: proyeccion liviana para el modulo Deliveries
        Task<OrderDeliveryInfo?> GetDeliveryInfoAsync(int orderId);
    }
}