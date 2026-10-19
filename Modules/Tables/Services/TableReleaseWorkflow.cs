using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Tables.DTOs;
using SaborExpress.Modules.Tables.Enum;
using SaborExpress.Modules.Tables.Mappings;
using SaborExpress.Modules.Tables.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Exceptions;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Helpers;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.Tables.Services
{
    public sealed class TableReleaseWorkflow
    {
        private readonly AppDbContext _db;
        private readonly IAuthRepository _users;
        private readonly IRealtimeNotifier _realtime;

        public TableReleaseWorkflow(AppDbContext db, IAuthRepository users, IRealtimeNotifier realtime)
        {
            _db = db;
            _users = users;
            _realtime = realtime;
        }

        private async Task<bool> CanAccessBranchAsync(int branchId, int userId, bool administrative)
        {
            var user = await _users.GetByIdWithRelationsAsync(userId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");
            if (user.HasRole(RoleNames.Gerente)) return true;
            if (user.Employee?.BranchId != branchId) return false;
            if (administrative) return user.HasRole(RoleNames.Administrador);
            return user.HasRole(RoleNames.Administrador) || user.HasRole(RoleNames.Cajero);
        }

        private async Task EnsureBranchAccessAsync(int branchId, int userId, bool administrative)
        {
            if (!await CanAccessBranchAsync(branchId, userId, administrative))
                throw new ForbiddenException("No tienes permiso para regularizar mesas de esta sede.");
        }

        // Solo pedidos de la sesión vinculada. Mesas antiguas sin seguimiento:
        // limitar a pedidos sin TableSessionId, nunca mezclar sesiones posteriores.
        private async Task<TableReleaseReviewDto> BuildReviewAsync(Table table)
        {
            var orders = await _db.Orders.AsNoTracking()
                .Where(o => o.TableId == table.Id &&
                    (table.CurrentSessionId.HasValue
                        ? o.TableSessionId == table.CurrentSessionId.Value
                        : o.TableSessionId == null))
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                .AsSplitQuery()
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var todayUtc = ColombiaTime.ToUtc(ColombiaTime.Now.Date);
            var review = new TableReleaseReviewDto
            {
                TableId = table.Id,
                TableNumber = table.Number,
                BranchId = table.BranchId,
                CurrentSessionId = table.CurrentSessionId,
                IsOccupied = table.Status == TableStatus.Occupied,
                // No fecha fiable para mesas huérfanas: no ofrecer cierre de "ayer".
                IsPreviousDay = orders.Count > 0 && orders.All(o => o.CreatedAt < todayUtc),
                IsOrphan = orders.Count == 0
            };

            foreach (var order in orders.Where(o => o.Status != OrderStatus.Cancelled))
            {
                var activeDetails = order.OrderDetails.Where(d =>
                    d.Status != OrderDetailStatus.Cancelled && d.Status != OrderDetailStatus.Voided).ToList();
                var delivered = activeDetails.Count > 0 &&
                    activeDetails.All(d => d.Status == OrderDetailStatus.Delivered);
                var amountPaid = order.Payments.Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);
                var paid = amountPaid >= order.Total;
                review.OutstandingAmount += Math.Max(0m, order.Total - amountPaid);
                review.Orders.Add(new TableReleaseOrderDto
                {
                    OrderId = order.Id,
                    CreatedAt = order.CreatedAt,
                    Status = order.Status.ToString(),
                    Total = order.Total,
                    PaidAmount = amountPaid,
                    IsDelivered = delivered,
                    IsPaid = paid
                });
            }

            // Pedidos cancelados no bloquean, pero al menos uno debe existir
            // en una sesión comprobada para evitar cierres de mesas huérfanas.
            review.CanReleaseNormally = review.IsOccupied && review.IsPreviousDay &&
                review.Orders.All(o => o.IsDelivered && o.IsPaid);
            review.RequiresAdministrativeRelease = review.IsOccupied &&
                (review.IsOrphan || (review.IsPreviousDay && !review.CanReleaseNormally));
            return review;
        }

        public async Task<TableReleaseReviewDto> GetReviewAsync(int tableId, int userId)
        {
            var table = await _db.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tableId && !t.IsDeleted)
                ?? throw new ArgumentException("La mesa no existe.");
            await EnsureBranchAccessAsync(table.BranchId, userId, false);
            return await BuildReviewAsync(table);
        }

        public async Task<List<TableReleaseReviewDto>> ListPreviousAsync(int branchId, int userId)
        {
            await EnsureBranchAccessAsync(branchId, userId, false);
            var tables = await _db.Tables.AsNoTracking()
                .Where(t => t.BranchId == branchId && !t.IsDeleted && t.Status == TableStatus.Occupied)
                .OrderBy(t => t.Number).ToListAsync();
            var results = new List<TableReleaseReviewDto>();
            foreach (var table in tables)
            {
                var review = await BuildReviewAsync(table);
                if (review.IsPreviousDay) results.Add(review);
            }
            return results;
        }

        public async Task<TableResponseDto> ReleasePreviousAsync(int tableId, ReleasePreviousTableDto dto, int userId)
        {
            if (dto == null) throw new ArgumentException("Debes confirmar la liberación.");
            var reason = dto.Reason?.Trim() ?? "";
            if (reason.Length > 500) throw new ArgumentException("El motivo supera los 500 caracteres.");
            if (dto.Administrative && reason.Length < 12)
                throw new ArgumentException("El cierre administrativo requiere un motivo de al menos 12 caracteres.");

            // SERIALIZABLE evita confirmar una previsualización obsoleta ante cambios
            // concurrentes en pedidos, pagos o sesión de mesa.
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            // Bloquear la mesa durante la regularización. No bloquea sesiones de otra sede.
            var table = (await _db.Tables.FromSqlInterpolated(
                $"SELECT * FROM tables WHERE id = {tableId} FOR UPDATE")
                .ToListAsync()).SingleOrDefault();
            if (table == null || table.IsDeleted)
                throw new ArgumentException("La mesa no existe.");
            await EnsureBranchAccessAsync(table.BranchId, userId, dto.Administrative);
            if (table.CurrentSessionId != dto.ExpectedSessionId)
                throw new InvalidOperationException("La sesión cambió. Actualiza los datos de la mesa antes de continuar.");

            var review = await BuildReviewAsync(table);
            if (!review.IsOccupied || (!review.IsPreviousDay && !(dto.Administrative && review.IsOrphan)))
                throw new InvalidOperationException("No puedes liberar una sesión activa de hoy.");
            if (!dto.Administrative && !review.CanReleaseNormally)
                throw new InvalidOperationException("Existen pedidos sin entregar, sin pagar o no hay historial verificable. Solicita una regularización administrativa.");

            _db.TableReleaseAudits.Add(new TableReleaseAudit
            {
                TableId = table.Id,
                TableNumber = table.Number,
                BranchId = table.BranchId,
                PreviousSessionId = table.CurrentSessionId,
                ActorUserId = userId,
                Administrative = dto.Administrative,
                Reason = dto.Administrative ? reason : "Cierre de jornada anterior: entregado y pagado",
                OrderIdsJson = JsonSerializer.Serialize(review.Orders.Select(o => o.OrderId)),
                OutstandingAmount = review.OutstandingAmount,
                ReleasedAtUtc = DateTime.UtcNow
            });
            table.Status = TableStatus.Available;
            table.CurrentSessionId = null;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await _realtime.TablesChangedAsync();
            await _realtime.OrdersChangedAsync();
            return TableMapper.ToResponse(table);
        }
    }
}
