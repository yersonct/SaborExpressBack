    // Modules/Orders/Repositories/OrderRepository.cs
    using Microsoft.EntityFrameworkCore;
    using SaborExpress.Data;
    using SaborExpress.Modules.Orders.Enum;
    using SaborExpress.Modules.Orders.Interfaces;
    using SaborExpress.Modules.Orders.Models;

    namespace SaborExpress.Modules.Orders.Repositories
    {
        public class OrderRepository : IOrderRepository
        {
            private readonly AppDbContext _context;

            public OrderRepository(AppDbContext context)
            {
                _context = context;
            }

            // Para el detalle completo (GetByIdAsync): sí necesita las líneas y productos.
            private IQueryable<Order> DetailQuery()
            {
                return _context.Orders
                    .Include(x => x.Customer)
                    .Include(x => x.Employee)
                    .Include(x => x.Table)
                    .Include(x => x.Branch)
                    .Include(x => x.OrderDetails)
                        .ThenInclude(d => d.Product);
            }

            // Para listados (resumen): NO necesita OrderDetails ni Product,
            // solo lo que OrderSummaryDto realmente usa. Incluye Payments
            // porque OrderMapper.ComputeIsFullyPaid necesita sumarlos.
            private IQueryable<Order> SummaryQuery()
            {
                return _context.Orders
                    .Include(x => x.Customer)
                    .Include(x => x.Employee)
                    .Include(x => x.Table)
                    .Include(x => x.OrderDetails)
                        .ThenInclude(d => d.Product)
                    .Include(x => x.Payments)
                    .AsNoTracking();
            }

            public async Task<Order?> GetByIdAsync(int id)
            {
                var order = await DetailQuery().FirstOrDefaultAsync(x => x.Id == id);
                if (order != null)
                    await AttachAssignedDeliveryInfoAsync(new[] { order });

                return order;
            }

            public async Task<List<Order>> GetAllAsync(int? branchId, OrderStatus? status, int? employeeId = null)
            {
                var query = SummaryQuery();

                if (branchId.HasValue)
                    query = query.Where(x => x.BranchId == branchId.Value);
                if (status.HasValue)
                    query = query.Where(x => x.Status == status.Value);
                if (employeeId.HasValue)
                    query = query.Where(x => x.EmployeeId == employeeId.Value);

                var orders = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
                await AttachAssignedDeliveryInfoAsync(orders);
                return orders;
            }

            public async Task<List<Order>> GetByTableIdAsync(int tableId)
            {
                var orders = await SummaryQuery()
                    .Where(x => x.TableId == tableId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

                await AttachAssignedDeliveryInfoAsync(orders);
                return orders;
            }


            public async Task<List<Order>> GetByCustomerIdAsync(int customerId)
            {
                var orders = await SummaryQuery()
                    .Where(x => x.CustomerId == customerId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

                await AttachAssignedDeliveryInfoAsync(orders);
                return orders;
            }

            public async Task<List<Order>> GetByBranchIdAsync(int branchId)
            {
                var orders = await SummaryQuery()
                    .Where(x => x.BranchId == branchId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

                await AttachAssignedDeliveryInfoAsync(orders);
                return orders;
            }

            public async Task<bool> CustomerExistsAsync(int customerId)
            {
                return await _context.Customers.AnyAsync(x => x.Id == customerId);
            }

            public async Task<bool> EmployeeExistsAsync(int employeeId)
            {
                return await _context.Employees.AnyAsync(x => x.Id == employeeId);
            }

            public async Task<bool> TableExistsInBranchAsync(int tableId, int branchId)
            {
                return await _context.Tables.AnyAsync(x => x.Id == tableId && x.BranchId == branchId);
            }

            public async Task<bool> BranchExistsAsync(int branchId)
            {
                return await _context.Branches.AnyAsync(x => x.Id == branchId);
            }

            public async Task AddAsync(Order order)
            {
                await _context.Orders.AddAsync(order);
            }

            public Task UpdateAsync(Order order)
            {
                _context.Orders.Update(order);
                return Task.CompletedTask;
            }

            public async Task SaveChangesAsync()
            {
                await _context.SaveChangesAsync();
            }
            private async Task AttachAssignedDeliveryInfoAsync(IEnumerable<Order> orders)
            {
                var deliveryOrderIds = orders
                    .Where(o => o.OrderType == OrderType.Delivery)
                    .Select(o => o.Id)
                    .ToList();

                if (deliveryOrderIds.Count == 0)
                    return;

                var deliveries = await _context.Deliveries
                    .Include(d => d.DeliveryPerson)
                    .Where(d => deliveryOrderIds.Contains(d.OrderId))
                    .ToListAsync();

                var byOrderId = deliveries.ToDictionary(d => d.OrderId);

                foreach (var order in orders)
                {
                    if (byOrderId.TryGetValue(order.Id, out var delivery))
                    {
                        order.AssignedDeliveryPersonId = delivery.DeliveryPersonId;
                        order.AssignedDeliveryPersonName =
                            $"{delivery.DeliveryPerson.Name} {delivery.DeliveryPerson.LastName}".Trim();
                    }
                }
            }
            public async Task RecalculateTotalsAsync(int orderId, decimal taxRate)
            {
                var subTotal = await _context.OrderDetails
                    .Where(d => d.OrderId == orderId
                            && d.Status != OrderDetailStatus.Voided
                            && d.Status != OrderDetailStatus.Cancelled)
                    .SumAsync(d => d.SubTotal);

                var tax = Math.Round(subTotal * taxRate, 2);
                var total = subTotal + tax;

                await _context.Orders
                    .Where(o => o.Id == orderId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.SubTotal, subTotal)
                        .SetProperty(o => o.Tax, tax)
                        .SetProperty(o => o.Total, total)
                        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));
            }

            public async Task<OrderStatus?> GetOrderStatusAsync(int orderId)
            {
                return await _context.Orders
                    .Where(o => o.Id == orderId)
                    .Select(o => (OrderStatus?)o.Status)
                    .FirstOrDefaultAsync();
            }

            public async Task<OrderDeliveryInfo?> GetDeliveryInfoAsync(int orderId)
            {
                return await _context.Orders
                    .Where(o => o.Id == orderId)
                    .Select(o => new OrderDeliveryInfo
                    {
                        Id = o.Id,
                        OrderType = o.OrderType,
                        Status = o.Status,
                        CustomerId = o.CustomerId,
                        BranchId = o.BranchId,
                        AddressId = o.AddressId, // 👈 NUEVO
                        Total = o.Total,
                        CreatedAt = o.CreatedAt
                    })
                    .FirstOrDefaultAsync();
            }
        }
    }