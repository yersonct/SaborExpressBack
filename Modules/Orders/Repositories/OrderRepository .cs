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

        private IQueryable<Order> BaseQuery()
        {
            return _context.Orders
                .Include(x => x.Customer)
                .Include(x => x.Employee)
                .Include(x => x.Table)
                .Include(x => x.Branch)
                .Include(x => x.OrderDetails)
                    .ThenInclude(d => d.Product);
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Order>> GetAllAsync(int? branchId, OrderStatus? status)
        {
            var query = BaseQuery().AsQueryable();

            if (branchId.HasValue)
                query = query.Where(x => x.BranchId == branchId.Value);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByTableIdAsync(int tableId)
        {
            return await BaseQuery()
                .Where(x => x.TableId == tableId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByCustomerIdAsync(int customerId)
        {
            return await BaseQuery()
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByBranchIdAsync(int branchId)
        {
            return await BaseQuery()
                .Where(x => x.BranchId == branchId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
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
        public async Task RecalculateTotalsAsync(int orderId, decimal taxRate)
        {
            var order = await _context.Orders
                .Include(x => x.OrderDetails)
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null) return;

            // Solo cuentan las líneas que siguen "vivas" — una anulada no debe cobrarse
            var subTotal = order.OrderDetails
                .Where(d => d.Status != OrderDetailStatus.Voided
                        && d.Status != OrderDetailStatus.Cancelled)
                .Sum(d => d.SubTotal);

            order.SubTotal = subTotal;
            order.Tax = Math.Round(subTotal * taxRate, 2);
            order.Total = order.SubTotal + order.Tax;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}