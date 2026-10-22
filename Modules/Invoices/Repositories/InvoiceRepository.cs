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

        // Los recibos necesitan los conceptos, el comprador y el pago. Las listas de
        // facturas mantienen una consulta más liviana para no cargar ese detalle en lote.
        private IQueryable<Invoice> DetailedQuery()
        {
            return _context.Invoices
                .Include(x => x.Branch)
                .Include(x => x.Order).ThenInclude(o => o.Customer).ThenInclude(c => c!.User)
                .Include(x => x.Order).ThenInclude(o => o.OrderDetails).ThenInclude(d => d.Product)
                .Include(x => x.Order).ThenInclude(o => o.Table)
                .Include(x => x.Order).ThenInclude(o => o.Address)
                .Include(x => x.Payment).ThenInclude(p => p.Cashier);
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            return await DetailedQuery().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Invoice?> GetByPaymentIdAsync(int paymentId)
        {
            return await DetailedQuery().FirstOrDefaultAsync(x => x.PaymentId == paymentId);
        }

        public async Task<List<Invoice>> GetByOrderIdAsync(int orderId)
        {
            return await DetailedQuery()
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

        public async Task AddWithAssignedNumberAsync(Invoice invoice)
        {
            // The transaction keeps the temporary number invisible. Using the
            // generated identity avoids count+1 races when two cashiers invoice
            // different payments at the same time.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                invoice.InvoiceNumber = $"PENDING-{Guid.NewGuid():N}";
                await _context.Invoices.AddAsync(invoice);
                await _context.SaveChangesAsync();

                invoice.InvoiceNumber = $"SUC{invoice.BranchId:000}-{invoice.Id:000000}";
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                _context.Entry(invoice).State = EntityState.Detached;
                throw;
            }
        }
    }
}
