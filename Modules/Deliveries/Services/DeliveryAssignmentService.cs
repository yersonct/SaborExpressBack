using System.Collections.Concurrent;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Deliveries.Enum;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Shared.Helpers;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.Deliveries.Services
{
    public class DeliveryAssignmentService : IDeliveryAssignmentService
    {
        private const int DefaultMaxActiveDeliveriesPerCourier = 4;
        private static readonly TimeSpan FreshLocationWindow = TimeSpan.FromMinutes(5);
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> BranchLocks = new();

        private readonly IDeliveryRepository _deliveryRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IBranchOperationalSettingsRepository _settingsRepository;
        private readonly IUserNotificationService _notificationService;
        private readonly IAuthRepository _authRepository;
        private readonly IRealtimeNotifier _realtime;
        private readonly ILogger<DeliveryAssignmentService> _logger;

        public DeliveryAssignmentService(
            IDeliveryRepository deliveryRepository,
            IOrderRepository orderRepository,
            IBranchRepository branchRepository,
            IBranchOperationalSettingsRepository settingsRepository,
            IUserNotificationService notificationService,
            IAuthRepository authRepository,
            IRealtimeNotifier realtime,
            ILogger<DeliveryAssignmentService> logger)
        {
            _deliveryRepository = deliveryRepository;
            _orderRepository = orderRepository;
            _branchRepository = branchRepository;
            _settingsRepository = settingsRepository;
            _notificationService = notificationService;
            _authRepository = authRepository;
            _realtime = realtime;
            _logger = logger;
        }

        public async Task AssignAutomaticallyAsync(int orderId)
        {
            var initialOrder = await _orderRepository.GetDeliveryInfoAsync(orderId);
            if (initialOrder == null || initialOrder.OrderType != OrderType.Delivery)
            {
                _logger.LogInformation("Asignación omitida: el pedido no existe o no es un domicilio.");
                return;
            }

            var gate = BranchLocks.GetOrAdd(initialOrder.BranchId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                await AssignOrderUnderBranchLockAsync(orderId, initialOrder.BranchId);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task AssignPendingForBranchAsync(
            int branchId,
            int? retryNotificationForOrderId = null,
            bool throwOnNotificationFailure = false)
        {
            var gate = BranchLocks.GetOrAdd(branchId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                var orderIds = await _deliveryRepository.GetUnassignedDeliveryOrderIdsAsync(branchId);
                if (retryNotificationForOrderId.HasValue)
                    orderIds = orderIds.Append(retryNotificationForOrderId.Value).Distinct().ToList();

                foreach (var orderId in orderIds)
                    await AssignOrderUnderBranchLockAsync(
                        orderId,
                        branchId,
                        retryNotificationForOrderId == orderId,
                        throwOnNotificationFailure);
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task AssignOrderUnderBranchLockAsync(
            int orderId,
            int branchId,
            bool retryExistingNotification = false,
            bool throwOnNotificationFailure = false)
        {
            // Releer dentro del bloqueo evita asignar dos veces si pago y confirmación
            // intentan procesar el mismo pedido casi al mismo tiempo.
            var order = await _orderRepository.GetDeliveryInfoAsync(orderId);
            if (order == null
                || order.OrderType != OrderType.Delivery
                || order.BranchId != branchId
                || order.AddressId is null
                || (order.Status != OrderStatus.Confirmed
                    && order.Status != OrderStatus.InPreparation
                    && order.Status != OrderStatus.Ready))
                return;

            var existingDeliveries = await _deliveryRepository.GetByOrderIdAsync(orderId);
            if (existingDeliveries.Count > 0)
            {
                if (retryExistingNotification)
                    await NotifyAssignedCourierAsync(
                        existingDeliveries[0], orderId, throwOnNotificationFailure);
                return;
            }

            var settings = await _settingsRepository.GetByBranchIdAsync(branchId);
            var maxActiveDeliveries = settings?.MaxActiveDeliveriesPerCourier
                ?? DefaultMaxActiveDeliveriesPerCourier;
            var candidates = await _deliveryRepository.GetAvailableDeliveryPersonsWithLoadAsync(branchId);
            var branch = await _branchRepository.GetByIdAsync(branchId);

            var now = DateTime.UtcNow;
            var chosen = candidates
                .Where(c => c.ActiveDeliveryCount < maxActiveDeliveries)
                .Select(c => new
                {
                    Courier = c,
                    DistanceKm = GetFreshDistanceKm(c, branch, now)
                })
                // Primero toma la ubicación reciente más cercana a la sede. Si no
                // hay GPS vigente, usa la carga y después el ID como desempate estable.
                .OrderByDescending(x => x.DistanceKm.HasValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.Courier.ActiveDeliveryCount)
                .ThenBy(x => x.Courier.EmployeeId)
                .Select(x => x.Courier)
                .FirstOrDefault();

            if (chosen == null)
            {
                _logger.LogInformation("Un pedido queda en cola porque no hay repartidor disponible con capacidad.");
                return;
            }

            var delivery = new Delivery
            {
                OrderId = orderId,
                AddressId = order.AddressId.Value,
                DeliveryPersonId = chosen.EmployeeId,
                Status = DeliveryStatus.Assigned,
                AssignedAt = now
            };

            await _deliveryRepository.AddAsync(delivery);
            await _deliveryRepository.SaveChangesAsync();

            // La persistencia ya terminó: notificar a móvil y panel web.
            try
            {
                await _realtime.DeliveriesChangedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Entrega asignada, pero falló el evento en tiempo real ({ErrorType}).", ex.GetType().Name);
            }

            _logger.LogInformation(
                "Pedido asignado al repartidor, que tenía {ActiveCount} domicilios activos.",
                chosen.ActiveDeliveryCount);

            await NotifyAssignedCourierAsync(delivery, orderId, throwOnNotificationFailure);
        }

        private async Task NotifyAssignedCourierAsync(
            Delivery delivery, int orderId, bool throwOnNotificationFailure)
        {
            try
            {
                var courierUserId = await _authRepository.GetUserIdByEmployeeIdAsync(delivery.DeliveryPersonId);
                if (courierUserId == null)
                {
                    if (throwOnNotificationFailure)
                        throw new SaborExpress.Shared.Exceptions.ConflictException("El repartidor asignado no tiene usuario asociado.");
                    return;
                }

                await _notificationService.CreateAsync(
                    courierUserId.Value,
                    "Nuevo domicilio asignado",
                    "Se te asignó un pedido para entrega.",
                    SaborExpress.Modules.Notifications.Enum.NotificationType.DeliveryAssigned,
                    relatedEntityType: "Order",
                    relatedEntityId: orderId,
                    idempotencyKey: $"delivery:{delivery.Id}:assigned:{courierUserId.Value}");
            }
            catch (Exception exception)
            {
                // El Wompi outbox reintentará este aviso por pedido cuando se solicite.
                _logger.LogWarning("No se pudo guardar el aviso del domicilio ({ErrorType}).",
                    exception.GetType().Name);
                if (throwOnNotificationFailure)
                    throw;
            }
        }

        private static double? GetFreshDistanceKm(
            DeliveryPersonLoad courier,
            Modules.Branches.Models.Branch? branch,
            DateTime now)
        {
            if (branch == null
                || branch.Latitude == 0 && branch.Longitude == 0
                || courier.CurrentLatitude is null
                || courier.CurrentLongitude is null
                || courier.LocationUpdatedAt is null
                || now - courier.LocationUpdatedAt.Value > FreshLocationWindow)
                return null;

            return GeoDistance.Km(
                (double)branch.Latitude,
                (double)branch.Longitude,
                (double)courier.CurrentLatitude.Value,
                (double)courier.CurrentLongitude.Value);
        }
    }
}
