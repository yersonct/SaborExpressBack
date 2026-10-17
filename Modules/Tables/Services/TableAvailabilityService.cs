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

        public async Task<bool> TryReleaseAfterConfirmationAsync(int orderId)
        {
            var triggeringOrder = await _orderRepository.GetByIdAsync(orderId);
            if (triggeringOrder?.TableId is not int tableId)
                return false;

            var table = await _tableRepository.GetByIdAsync(tableId);
            if (table == null || table.Status != TableStatus.Occupied)
                return false;


            if (table.CurrentSessionId.HasValue)
            {
                if (triggeringOrder.TableSessionId != table.CurrentSessionId)
                    return false;
            }
            else if (triggeringOrder.TableSessionId.HasValue)
            {
                // Nunca permitir que una confirmacion de otra sesion libere esta mesa.
                return false;
            }

            var tableOrders = (await _orderRepository.GetByTableIdAsync(tableId))
                .Where(order =>
                    order.Status != OrderStatus.Cancelled &&
                    (!table.CurrentSessionId.HasValue ||
                     order.TableSessionId == table.CurrentSessionId))
                .ToList();


            if (tableOrders.Count == 0 ||
                !await AreAllOrdersDeliveredAndPaidAsync(tableOrders))
            {
                return false;
            }

            table.Status = TableStatus.Available;
            table.CurrentSessionId = null;

            await _tableRepository.UpdateAsync(table);
            await _tableRepository.SaveChangesAsync();
            await _realtime.TablesChangedAsync();

            var waiterUserIds = tableOrders
                .Where(order => order.Employee?.UserId > 0)
                .Select(order => order.Employee!.UserId)
                .Distinct();

            foreach (var waiterUserId in waiterUserIds)
            {
                try
                {
                    await _notificationService.CreateAsync(
                        waiterUserId,
                        "Mesa disponible",
                        $"La mesa {table.Number} quedó disponible: el pedido fue entregado y pagado.",
                        NotificationType.TableAvailable,
                        relatedEntityType: "Order",
                        relatedEntityId: triggeringOrder.Id);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception,
                        "La mesa {TableId} quedó disponible, pero no se pudo notificar al mesero {UserId}.",
                        tableId,
                        waiterUserId);
                }
            }

            return true;
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
                // Mesa huerfana: no hay pedidos asociados a esta atencion.
                // Permite al cajero restaurar Disponible sin inventar una sesion.
                return;
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
