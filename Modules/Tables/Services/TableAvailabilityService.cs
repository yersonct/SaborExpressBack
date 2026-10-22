using SaborExpress.Modules.Notifications.Enum;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Tables.Enum;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.Tables.Services
{
    public sealed class TableAvailabilityService : ITableAvailabilityService
    {
        private readonly ITableRepository _tableRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderDetailRepository _orderDetailRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUserNotificationService _notificationService;
        private readonly IRealtimeNotifier _realtime;
        private readonly ILogger<TableAvailabilityService> _logger;

        public TableAvailabilityService(
            ITableRepository tableRepository,
            IOrderRepository orderRepository,
            IOrderDetailRepository orderDetailRepository,
            IPaymentRepository paymentRepository,
            IUserNotificationService notificationService,
            IRealtimeNotifier realtime,
            ILogger<TableAvailabilityService> logger)
        {
            _tableRepository = tableRepository;
            _orderRepository = orderRepository;
            _orderDetailRepository = orderDetailRepository;
            _paymentRepository = paymentRepository;
            _notificationService = notificationService;
            _realtime = realtime;
            _logger = logger;
        }

        public async Task<bool> TryReleaseAfterConfirmationAsync(
            int orderId, bool throwOnNotificationFailure = false)
        {
            var triggeringOrder = await _orderRepository.GetByIdAsync(orderId);
            if (triggeringOrder?.TableId is not int tableId)
                return false;

            var table = await _tableRepository.GetByIdAsync(tableId);
            if (table == null ||
                (table.Status != TableStatus.Occupied && table.Status != TableStatus.Available))
                return false;

            if (table.CurrentSessionId.HasValue &&
                triggeringOrder.TableSessionId != table.CurrentSessionId)
                return false;

            // Una mesa ocupada sin sesión corresponde al esquema anterior. No se debe
            // liberar con un pedido de una sesión histórica; una mesa ya liberada sí
            // puede procesar de nuevo sus avisos pendientes de esa sesión.
            if (table.Status == TableStatus.Occupied && !table.CurrentSessionId.HasValue &&
                triggeringOrder.TableSessionId.HasValue)
                return false;

            var sessionId = table.CurrentSessionId ?? triggeringOrder.TableSessionId;
            var tableOrders = (await _orderRepository.GetByTableIdAsync(tableId))
                .Where(order => order.Status != OrderStatus.Cancelled &&
                    (sessionId.HasValue
                        ? order.TableSessionId == sessionId
                        : order.TableSessionId == null))
                .ToList();

            if (tableOrders.Count == 0 ||
                !await AreAllOrdersDeliveredAndPaidAsync(tableOrders))
                return false;

            var wasOccupied = table.Status == TableStatus.Occupied;
            if (wasOccupied)
            {
                table.Status = TableStatus.Available;
                table.CurrentSessionId = null;
                await _tableRepository.UpdateAsync(table);
                await _tableRepository.SaveChangesAsync();
            }

            var waiterUserIds = tableOrders
                .Where(order => order.Employee?.UserId > 0)
                .Select(order => order.Employee!.UserId)
                .Distinct();

            foreach (var waiterUserId in waiterUserIds)
            {
                try
                {
                    var sessionKey = sessionId.HasValue
                        ? $"session:{sessionId.Value:N}"
                        : $"legacy-order:{triggeringOrder.Id}";
                    await _notificationService.CreateAsync(
                        waiterUserId,
                        "Mesa disponible",
                        $"La mesa {table.Number} quedó disponible: el pedido fue entregado y pagado.",
                        NotificationType.TableAvailable,
                        relatedEntityType: "Order",
                        relatedEntityId: triggeringOrder.Id,
                        idempotencyKey: $"table:{tableId}:{sessionKey}:available:{waiterUserId}");
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        "No se pudo guardar el aviso de mesa disponible ({ErrorType}).",
                        exception.GetType().Name);
                    if (throwOnNotificationFailure)
                        throw;
                }
            }

            if (wasOccupied)
            {
                try
                {
                    await _realtime.TablesChangedAsync();
                }
                catch (Exception exception)
                {
                    // La mesa y sus avisos están persistidos; SignalR es un aviso efímero.
                    _logger.LogWarning("Falló la actualización SignalR de mesas ({ErrorType}).",
                        exception.GetType().Name);
                }
            }

            return wasOccupied;
        }

        public async Task EnsureCanSetAvailableAsync(int tableId)
        {
            var table = await _tableRepository.GetByIdAsync(tableId)
                ?? throw new ArgumentException("La mesa no existe.");

            // Compatibilidad con mesas ocupadas antes de crear el seguimiento de sesiones.
            // Sin sesion comprobamos TODOS los pedidos no cancelados de la mesa.

            var tableOrders = (await _orderRepository.GetByTableIdAsync(tableId))
                .Where(order =>
                    order.Status != OrderStatus.Cancelled &&
                    (!table.CurrentSessionId.HasValue ||
                     order.TableSessionId == table.CurrentSessionId))
                .ToList();

            if (tableOrders.Count == 0)
            {
                // Sin pedidos no hay evidencia para una liberación ordinaria.
                // La revisión administrativa exige motivo y deja auditoría.
                throw new InvalidOperationException(
                    "La mesa no tiene pedidos asociados. Solicita un cierre administrativo con motivo.");
            }

            if (tableOrders.Count > 0 &&
                !await AreAllOrdersDeliveredAndPaidAsync(tableOrders))
            {
                throw new InvalidOperationException(
                    "No se puede liberar la mesa hasta que todos los productos estén entregados al mesero y los pedidos estén pagados.");
            }
        }

        private async Task<bool> AreAllOrdersDeliveredAndPaidAsync(
            IReadOnlyCollection<Order> orders)
        {
            foreach (var order in orders)
            {
                var details = await _orderDetailRepository.GetByOrderIdAsync(order.Id);
                var activeDetails = details
                    .Where(detail => detail.Status != OrderDetailStatus.Voided &&
                                     detail.Status != OrderDetailStatus.Cancelled)
                    .ToList();

                // Una orden vacía no cuenta como entregada.
                if (activeDetails.Count == 0 ||
                    activeDetails.Any(detail => detail.Status != OrderDetailStatus.Delivered))
                {
                    return false;
                }

                var payments = await _paymentRepository.GetByOrderIdAsync(order.Id);
                var paidAmount = payments
                    .Where(payment => payment.Status == PaymentStatus.Completed)
                    .Sum(payment => payment.Amount);

                if (paidAmount < order.Total)
                    return false;
            }

            return true;
        }
    }
}
