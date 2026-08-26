// Modules/Deliveries/Repositories/DeliveryRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Modules.Deliveries.Repositories
{
    public class DeliveryRepository : IDeliveryRepository
    {
        private readonly AppDbContext _context;

        public DeliveryRepository(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<Delivery> BaseQuery()
        {
            return _context.Deliveries
                .Include(x => x.Address)
                .Include(x => x.DeliveryPerson);
        }

        public async Task<Delivery?> GetByIdAsync(int id)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Delivery>> GetByOrderIdAsync(int orderId)
        {
            return await BaseQuery()
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.AssignedAt)
                .ToListAsync();
        }

        public async Task<List<Delivery>> GetByDeliveryPersonIdAsync(int deliveryPersonId)
        {
            return await BaseQuery()
                .Where(x => x.DeliveryPersonId == deliveryPersonId)
                .OrderByDescending(x => x.AssignedAt)
                .ToListAsync();
        }

        // NUEVO: pedidos listos, tipo Delivery, sin repartidor asignado todavía
        public async Task<List<Order>> GetAvailableOrdersAsync()
        {
            return await _context.Orders
                .Where(o => o.OrderType == OrderType.Delivery
                    && (o.Status == OrderStatus.Ready || o.Status == OrderStatus.Confirmed)
                    && !_context.Deliveries.Any(d => d.OrderId == o.Id))
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
        }

        // NUEVO
        public async Task<int?> GetEmployeeBranchIdAsync(int employeeId)
        {
            return await _context.Employees
                .Where(e => e.Id == employeeId)
                .Select(e => (int?)e.BranchId)
                .FirstOrDefaultAsync();
        }

        // NUEVO: entregas de todos los repartidores de una sede
        public async Task<List<Delivery>> GetByBranchIdAsync(int branchId)
        {
            return await BaseQuery()
                .Where(x => x.DeliveryPerson.BranchId == branchId)
                .OrderByDescending(x => x.AssignedAt)
                .ToListAsync();
        }

        public async Task<bool> OrderExistsAsync(int orderId)
        {
            return await _context.Orders.AnyAsync(x => x.Id == orderId);
        }

        public async Task<bool> AddressExistsAsync(int addressId)
        {
            return await _context.Addresses.AnyAsync(x => x.Id == addressId);
        }

        public async Task<bool> AddressBelongsToCustomerAsync(int addressId, int customerId)
        {
            return await _context.Addresses
                .AnyAsync(a => a.Id == addressId && a.CustomerId == customerId);
        }

        public async Task<bool> OrderHasDeliveryAsync(int orderId)
        {
            return await _context.Deliveries.AnyAsync(x => x.OrderId == orderId);
        }

        public async Task<bool> EmployeeHasDeliveryRoleAsync(int employeeId)
        {
            return await _context.Employees
                .Where(e => e.Id == employeeId)
                .SelectMany(e => e.User.UserRoles)
                .AnyAsync(ur => ur.Role.Name == RoleNames.Repartidor);
        }

        public async Task AddAsync(Delivery delivery)
        {
            await _context.Deliveries.AddAsync(delivery);
        }

        public Task UpdateAsync(Delivery delivery)
        {
            _context.Deliveries.Update(delivery);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}