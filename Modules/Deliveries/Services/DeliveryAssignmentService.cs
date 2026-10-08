// Modules/Deliveries/Services/DeliveryAssignmentService.cs
using SaborExpress.Modules.Deliveries.Enum;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Auth.Interfaces;

namespace SaborExpress.Modules.Deliveries.Services
{
    public class DeliveryAssignmentService : IDeliveryAssignmentService
    {
        private const int MaxActiveDeliveriesPerPerson = 4;

        private readonly IDeliveryRepository _deliveryRepository;
        private readonly IOrderRepository _orderRepository;

        private readonly IUserNotificationService _notificationService;
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<DeliveryAssignmentService> _logger;

        public DeliveryAssignmentService(
            IDeliveryRepository deliveryRepository,
            IOrderRepository orderRepository,
            IUserNotificationService notificationService,
            IAuthRepository authRepository,
            ILogger<DeliveryAssignmentService> logger)
        {
            _deliveryRepository = deliveryRepository;
            _orderRepository = orderRepository;
            _notificationService = notificationService;
            _authRepository = authRepository;
            _logger = logger;
        }

        public async Task AssignAutomaticallyAsync(int orderId)
        {
            var order = await _orderRepository.GetDeliveryInfoAsync(orderId);
            if (order == null || order.OrderType != OrderType.Delivery)
            {
                _logger.LogInformation("Asignación omitida: pedido {OrderId} no existe o no es Delivery.", orderId);
                return;
            }

            if (order.AddressId is null)
            {
                _logger.LogWarning("Asignación omitida: pedido {OrderId} sin dirección.", orderId);
                return;
            }

            if (await _deliveryRepository.OrderHasDeliveryAsync(orderId))
            {
                _logger.LogInformation("Pedido {OrderId} ya tiene repartidor asignado.", orderId);
                return;
            }

            var candidates = await _deliveryRepository.GetAvailableDeliveryPersonsWithLoadAsync(order.BranchId);
            if (candidates.Count == 0)
            {
                _logger.LogWarning(
                    "Pedido {OrderId} (sede {BranchId}): NO hay repartidores Activos y Disponibles en esa sede. Queda sin asignar.",
                    orderId, order.BranchId);
                return;
            }

            // Nivel 1: de los que tienen menos de 4 entregas activas, el de menor carga.
            var chosen = candidates
                .Where(c => c.ActiveDeliveryCount < MaxActiveDeliveriesPerPerson)
                .OrderBy(c => c.ActiveDeliveryCount)
                .FirstOrDefault();

            // Nivel 2: si nadie cumple la regla, se asigna igual al de menor carga.
            chosen ??= candidates.OrderBy(c => c.ActiveDeliveryCount).First();

            var delivery = new Delivery
            {
                OrderId = orderId,
                AddressId = order.AddressId.Value,
                DeliveryPersonId = chosen.EmployeeId,
                Status = DeliveryStatus.Assigned,
                AssignedAt = DateTime.UtcNow
            };

            await _deliveryRepository.AddAsync(delivery);
            await _deliveryRepository.SaveChangesAsync();

            _logger.LogInformation("Pedido {OrderId} asignado al repartidor (employee) {EmployeeId}.",
                orderId, chosen.EmployeeId);

            try
            {
                var deliveryPersonUserId = await _authRepository.GetUserIdByEmployeeIdAsync(chosen.EmployeeId);
                if (deliveryPersonUserId == null)
                {
                    _logger.LogWarning("El empleado {EmployeeId} no tiene UserId; no se pudo notificar.", chosen.EmployeeId);
                    return;
                }

                await _notificationService.CreateAsync(
                    deliveryPersonUserId.Value,
                    "Nuevo domicilio asignado",
                    $"Se te asignó el pedido #{orderId} para entrega.",
                    SaborExpress.Modules.Notifications.Enum.NotificationType.DeliveryAssigned,
                    relatedEntityType: "Order",
                    relatedEntityId: orderId);
            }
            catch (Exception ex)
            {
                // La entrega ya quedó guardada; un fallo al notificar no debe romperla.
                _logger.LogError(ex, "No se pudo notificar al repartidor del pedido {OrderId}", orderId);
            }
        }
                // Asigna los pedidos Delivery confirmados que quedaron sin repartidor.
        // Se llama cuando un repartidor se marca disponible.
        public async Task AssignPendingForBranchAsync(int branchId)
        {
            var orderIds = await _deliveryRepository.GetUnassignedDeliveryOrderIdsAsync(branchId);
            foreach (var orderId in orderIds)
                await AssignAutomaticallyAsync(orderId); // cada asignación guarda, así la carga se actualiza
        }
    }
}