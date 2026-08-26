// Modules/Invoices/Repositories/InvoiceRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Invoices.Interfaces;
using SaborExpress.Modules.Invoices.Models;

namespace SaborExpress.Modules.Invoices.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly AppDbContext _context;

        public InvoiceRepository(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<Invoice> BaseQuery()
        {
            return _context.Invoices
                .Include(x => x.Branch)
                .Include(x => x.Order)
                    .ThenInclude(o => o.Customer);
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Invoice?> GetByPaymentIdAsync(int paymentId)
        {
            return await BaseQuery().FirstOrDefaultAsync(x => x.PaymentId == paymentId);
        }

        public async Task<List<Invoice>> GetByOrderIdAsync(int orderId)
        {
            return await BaseQuery()
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.IssuedAt)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetByBranchIdAsync(int branchId)
        {
            return await BaseQuery()
                .Where(x => x.BranchId == branchId)
                .OrderByDescending(x => x.IssuedAt)
                .ToListAsync();
        }

        public async Task<int> GetNextInvoiceNumberAsync(int branchId)
        {
            var count = await _context.Invoices
                .CountAsync(x => x.BranchId == branchId);

            return count + 1;
        }

        public async Task AddAsync(Invoice invoice)
        {
            await _context.Invoices.AddAsync(invoice);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
