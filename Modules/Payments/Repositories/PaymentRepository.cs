// Modules/Payments/Repositories/PaymentRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Modules.Payments.Enum;

namespace SaborExpress.Modules.Payments.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _context.Payments
                .Include(x => x.Cashier)
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.Id == id);
        }
                public async Task<List<Payment>> GetPendingWompiSinceAsync(DateTime since)
        {
            return await _context.Payments
                .Include(x => x.Order)
                .Where(p => p.Method == PaymentMethod.Wompi
                         && p.Status == PaymentStatus.Pending
                         && p.WompiReference != null
                         && p.PaidAt >= since)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Include(x => x.Cashier)
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.PaidAt)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetAllAsync(int? branchId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Payments
                .Include(x => x.Cashier)
                .Include(x => x.Order)
                .AsQueryable();

            if (branchId.HasValue)
                query = query.Where(x => x.Order.BranchId == branchId.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.PaidAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.PaidAt <= toDate.Value);

            return await query
                .OrderByDescending(x => x.PaidAt)
                .ToListAsync();
        }

        // NUEVO — cierre de caja: pagos que ESTE cajero registró desde una fecha (hoy, normalmente).
        public async Task<List<Payment>> GetByCashierSinceAsync(int cashierId, DateTime since)
        {
            return await _context.Payments
                .Include(x => x.Cashier)
                .Where(x => x.CashierId == cashierId && x.PaidAt >= since)
                .OrderByDescending(x => x.PaidAt)
                .ToListAsync();
        }

        public async Task<bool> OrderExistsAsync(int orderId)
        {
            return await _context.Orders.AnyAsync(x => x.Id == orderId);
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Payment?> GetByWompiReferenceAsync(string reference)
        {
            return await _context.Payments
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.WompiReference == reference);
        }
    }
}