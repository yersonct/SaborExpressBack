// Modules/Deliveries/Repositories/DeliveryRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Deliveries.Enum;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Modules.Payments.Enum;

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
                .Include(x => x.DeliveryPerson)
                .Include(x => x.Order)
                    .ThenInclude(o => o.Customer)
                .Include(x => x.Order)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(d => d.Product);
        }

        // NUEVO: Order no tiene navegación hacia Payment, así que se resuelve aparte.
        // Si un pedido tiene varios pagos (ej. un intento fallido + uno exitoso),
        // se toma el más reciente por PaidAt.
        private async Task AttachPaymentInfoAsync(IEnumerable<Delivery> deliveries)
        {
            var orderIds = deliveries.Select(d => d.OrderId).Distinct().ToList();
            if (orderIds.Count == 0)
                return;

            var payments = await _context.Payments
                .Where(p => orderIds.Contains(p.OrderId))
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            var latestByOrderId = payments
                .GroupBy(p => p.OrderId)
                .ToDictionary(g => g.Key, g => g.First());

            var paidByOrderId = payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .GroupBy(p => p.OrderId)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            foreach (var delivery in deliveries)
            {
                delivery.AmountPaid = paidByOrderId.TryGetValue(delivery.OrderId, out var paid) ? paid : 0;

                if (latestByOrderId.TryGetValue(delivery.OrderId, out var payment))
                {
                    delivery.PaymentMethod = payment.Method.ToString();
                    delivery.PaymentStatus = payment.Status.ToString();
                }
                // Si no hay pago todavía (ej. contra entrega, aún no cobrado),
                // quedan en null — el frontend lo interpreta como "pendiente de cobro".
            }
        }

        public async Task<Delivery?> GetByIdAsync(int id)
        {
            var delivery = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
            if (delivery != null)
                await AttachPaymentInfoAsync(new[] { delivery });

            return delivery;
        }

        public async Task<List<Delivery>> GetByOrderIdAsync(int orderId)
        {
            var deliveries = await BaseQuery()
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.AssignedAt)
                .ToListAsync();

            await AttachPaymentInfoAsync(deliveries);
            return deliveries;
        }

        public async Task<List<Delivery>> GetByDeliveryPersonIdAsync(int deliveryPersonId)
        {
            var deliveries = await BaseQuery()
                .Where(x => x.DeliveryPersonId == deliveryPersonId)
                .OrderByDescending(x => x.AssignedAt)
                .ToListAsync();

            await AttachPaymentInfoAsync(deliveries);
            return deliveries;
        }

        public async Task<List<DeliveryPersonLoad>> GetAvailableDeliveryPersonsWithLoadAsync(int branchId)
        {
            return await _context.Employees
                .Where(e => e.BranchId == branchId
                    && e.Status == "Activo"
                    && e.IsAvailable
                    && e.User.UserRoles.Any(ur => ur.Role.Name == RoleNames.Repartidor))
                .Select(e => new DeliveryPersonLoad
                {
                    EmployeeId = e.Id,
                    ActiveDeliveryCount = _context.Deliveries
                        .Count(d => d.DeliveryPersonId == e.Id
                            && d.Status != DeliveryStatus.Delivered
                            && d.Order.Status != OrderStatus.Cancelled)
                })
                .ToListAsync();
        }

        public async Task<int?> GetEmployeeBranchIdAsync(int employeeId)
        {
            return await _context.Employees
                .Where(e => e.Id == employeeId)
                .Select(e => (int?)e.BranchId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Delivery>> GetByBranchIdAsync(int branchId)
        {
            var deliveries = await BaseQuery()
                .Where(x => x.DeliveryPerson.BranchId == branchId)
                .OrderByDescending(x => x.AssignedAt)
                .ToListAsync();

            await AttachPaymentInfoAsync(deliveries);
            return deliveries;
        }
                public async Task<bool> IsDeliveryPersonAsync(int employeeId)
        {
            return await _context.Employees.AnyAsync(e =>
                e.Id == employeeId
                && e.Status == "Activo"
                && e.User.UserRoles.Any(ur => ur.Role.Name == RoleNames.Repartidor));
        }

        // Todos los repartidores activos de la sede (incluye los no disponibles),
        // para que el gerente elija a mano.
        public async Task<List<DeliveryPersonSummary>> GetDeliveryPersonsByBranchAsync(int branchId)
        {
            return await _context.Employees
                .Where(e => e.BranchId == branchId
                    && e.Status == "Activo"
                    && e.User.UserRoles.Any(ur => ur.Role.Name == RoleNames.Repartidor))
                .Select(e => new DeliveryPersonSummary
                {
                    EmployeeId = e.Id,
                    Name = e.Name,
                    LastName = e.LastName,
                    IsAvailable = e.IsAvailable,
                    ActiveDeliveryCount = _context.Deliveries
                        .Count(d => d.DeliveryPersonId == e.Id
                            && d.Status != DeliveryStatus.Delivered
                            && d.Order.Status != OrderStatus.Cancelled)
                })
                .OrderBy(s => s.ActiveDeliveryCount)
                .ToListAsync();
        }

        // Pedidos Delivery confirmados de la sede que todavía no tienen repartidor
        public async Task<List<int>> GetUnassignedDeliveryOrderIdsAsync(int branchId)
        {
            return await _context.Orders
                .Where(o => o.BranchId == branchId
                    && o.OrderType == OrderType.Delivery
                    && o.Status == OrderStatus.Confirmed
                    && o.AddressId != null
                    && !_context.Deliveries.Any(d => d.OrderId == o.Id))
                .OrderBy(o => o.CreatedAt)
                .Select(o => o.Id)
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