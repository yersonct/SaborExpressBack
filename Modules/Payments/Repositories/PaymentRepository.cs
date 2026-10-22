// Modules/Payments/Repositories/PaymentRepository.cs
using Microsoft.EntityFrameworkCore;
using System.Data;
using SaborExpress.Data;
using SaborExpress.Modules.Orders.Enum;
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
                    .ThenInclude(o => o.Customer)
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
                .OrderBy(p => p.PaidAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetWompiPostProcessingPendingAsync(int limit)
        {
            var staleBeforeUtc = DateTime.UtcNow.AddMinutes(-5);
            return await _context.Payments
                .AsNoTracking()
                .Where(payment => payment.Method == PaymentMethod.Wompi &&
                    payment.Status == PaymentStatus.Completed &&
                    payment.WompiTransactionId != null &&
                    payment.WompiPostProcessingCompletedAtUtc == null &&
                    (payment.WompiPostProcessingStartedAtUtc == null ||
                     payment.WompiPostProcessingStartedAtUtc < staleBeforeUtc))
                .OrderBy(payment => payment.PaidAt)
                .Take(Math.Clamp(limit, 1, 100))
                .ToListAsync();
        }

        public async Task<bool> TryClaimWompiPostProcessingAsync(
            int paymentId, DateTime staleBeforeUtc, DateTime nowUtc)
        {
            var updated = await _context.Payments
                .Where(payment => payment.Id == paymentId &&
                    payment.Method == PaymentMethod.Wompi &&
                    payment.Status == PaymentStatus.Completed &&
                    payment.WompiTransactionId != null &&
                    payment.WompiPostProcessingCompletedAtUtc == null &&
                    (payment.WompiPostProcessingStartedAtUtc == null ||
                     payment.WompiPostProcessingStartedAtUtc < staleBeforeUtc))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(payment => payment.WompiPostProcessingStartedAtUtc, nowUtc));

            return updated == 1;
        }

        public async Task MarkWompiPostProcessingCompletedAsync(int paymentId, DateTime completedAtUtc)
        {
            await _context.Payments
                .Where(payment => payment.Id == paymentId &&
                    payment.WompiPostProcessingCompletedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(payment => payment.WompiPostProcessingCompletedAtUtc, completedAtUtc)
                    .SetProperty(payment => payment.WompiPostProcessingStartedAtUtc, (DateTime?)null));
        }

        public async Task ReleaseWompiPostProcessingClaimAsync(int paymentId)
        {
            await _context.Payments
                .Where(payment => payment.Id == paymentId &&
                    payment.WompiPostProcessingCompletedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(payment => payment.WompiPostProcessingStartedAtUtc, (DateTime?)null));
        }

        public async Task<List<Payment>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Include(x => x.Cashier)
                .Include(x => x.Order)
                    .ThenInclude(o => o.Customer)
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.PaidAt)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetAllAsync(int? branchId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Payments
                .Include(x => x.Cashier)
                .Include(x => x.Order)
                    .ThenInclude(o => o.Customer)
                .AsNoTracking()
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
        public async Task<List<Payment>> GetByCashierInRangeAsync(int cashierId, DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Payments
                .Include(x => x.Cashier)
                .Where(x => x.CashierId == cashierId && x.PaidAt >= fromUtc && x.PaidAt < toUtc)
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

        public async Task<bool> TryAddCompletedPaymentAtomicallyAsync(Payment payment)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(order => order.Id == payment.OrderId)
                ?? throw new ArgumentException("El pedido no existe.");

            if (order.Status is OrderStatus.Pending or OrderStatus.Cancelled)
                throw new InvalidOperationException("Este pedido no admite pagos en su estado actual.");

            var existing = await _context.Payments
                .AsNoTracking()
                .Where(existingPayment => existingPayment.OrderId == payment.OrderId)
                .ToListAsync();

            var reserved = existing
                .Where(existingPayment => existingPayment.Status == PaymentStatus.Completed ||
                    (existingPayment.Method == PaymentMethod.Wompi &&
                     existingPayment.Status == PaymentStatus.Pending))
                .Sum(existingPayment => existingPayment.Amount);
            var remaining = order.Total - reserved;

            if (remaining <= 0 || payment.Amount <= 0 || payment.Amount > remaining)
            {
                await transaction.CommitAsync();
                return false;
            }

            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        public async Task<(Payment Payment, bool Created)> GetOrCreatePendingWompiPaymentAsync(
            Payment requestedPayment, OrderStatus? requiredOrderStatus)
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(order => order.Id == requestedPayment.OrderId)
                ?? throw new ArgumentException("El pedido no existe.");

            if (requiredOrderStatus.HasValue)
            {
                if (order.Status != requiredOrderStatus.Value)
                    throw new InvalidOperationException("El pedido ya no admite este pago en lÃ­nea.");
            }
            else if (order.Status is OrderStatus.Pending or OrderStatus.Cancelled)
            {
                throw new InvalidOperationException("Este pedido no admite pagos en su estado actual.");
            }

            var existing = await _context.Payments
                .AsNoTracking()
                .Where(payment => payment.OrderId == requestedPayment.OrderId)
                .ToListAsync();

            var completed = existing
                .Where(payment => payment.Status == PaymentStatus.Completed)
                .Sum(payment => payment.Amount);
            var pendingWompi = existing
                .Where(payment => payment.Method == PaymentMethod.Wompi &&
                    payment.Status == PaymentStatus.Pending)
                .ToList();

            if (completed >= order.Total)
                throw new InvalidOperationException("Este pedido ya estÃ¡ pagado.");

            if (pendingWompi.Count > 1)
                throw new InvalidOperationException(
                    "Este pedido ya tiene varios pagos Wompi pendientes y requiere revisiÃ³n.");

            var remaining = order.Total - completed;
            if (pendingWompi.Count == 1)
            {
                var current = pendingWompi[0];
                if (current.Amount != remaining || string.IsNullOrWhiteSpace(current.WompiReference))
                    throw new InvalidOperationException("Ya hay un pago Wompi pendiente para este pedido.");

                await transaction.CommitAsync();
                return (current, false);
            }

            if (requestedPayment.Amount != remaining)
                throw new InvalidOperationException("El saldo del pedido cambiÃ³. Actualiza el pedido e intÃ©ntalo de nuevo.");

            requestedPayment.Status = PaymentStatus.Pending;
            requestedPayment.Method = PaymentMethod.Wompi;
            await _context.Payments.AddAsync(requestedPayment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (requestedPayment, true);
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

        // Operación atómica en SQL: solo un webhook o sincronizador gana el cambio
        // de Pending a estado terminal, incluso si llegan peticiones concurrentes.
        public async Task<bool> TryTransitionWompiStatusAsync(
            int paymentId, PaymentStatus targetStatus, string transactionId)
        {
            if (targetStatus != PaymentStatus.Completed && targetStatus != PaymentStatus.Failed)
                throw new ArgumentOutOfRangeException(nameof(targetStatus));

            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("El ID de transacción Wompi es obligatorio.", nameof(transactionId));

            var updated = await _context.Payments
                .Where(p => p.Id == paymentId &&
                            p.Method == PaymentMethod.Wompi &&
                            p.Status == PaymentStatus.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, targetStatus)
                    .SetProperty(p => p.WompiTransactionId, transactionId));

            if (updated != 1)
                return false;

            // ExecuteUpdate no actualiza los objetos EF ya cargados en este scope.
            // Recargarlos evita que el siguiente SaveChanges restaure un estado antiguo.
            var tracked = _context.ChangeTracker.Entries<Payment>()
                .FirstOrDefault(e => e.Entity.Id == paymentId);
            if (tracked != null)
                await tracked.ReloadAsync();

            return true;
        }

        public async Task<Payment?> GetByWompiReferenceAsync(string reference)
        {
            return await _context.Payments
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.WompiReference == reference);
        }
    }
}
