// Modules/Deliveries/Services/DeliveryService.cs
using SaborExpress.Modules.Deliveries.DTOs;
using SaborExpress.Modules.Deliveries.Enum;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Mappings;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Deliveries.Validators;
using SaborExpress.Modules.Notifications.Enum;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Modules.Customers.Interfaces;

namespace SaborExpress.Modules.Deliveries.Services
{
    public class DeliveryService : IDeliveryService
    {
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly DeliveryValidator _validator;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserNotificationService _notificationService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<DeliveryService> _logger;
    private readonly IRealtimeNotifier _realtime;
    private readonly ICustomerRepository _customerRepository;
    private readonly IDeliveryAssignmentService _deliveryAssignmentService;

    public DeliveryService(
        IDeliveryRepository deliveryRepository,
        IOrderRepository orderRepository,
        IPaymentRepository paymentRepository,
        DeliveryValidator validator,
        IAuthorizationService authorizationService,
        IUserNotificationService notificationService,
        INotificationRepository notificationRepository,
        ILogger<DeliveryService> logger,
        IRealtimeNotifier realtime,
        ICustomerRepository customerRepository,
        IDeliveryAssignmentService deliveryAssignmentService)
    {
        _deliveryRepository = deliveryRepository;
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _validator = validator;
        _authorizationService = authorizationService;
        _notificationService = notificationService;
        _notificationRepository = notificationRepository;
        _logger = logger;
        _realtime = realtime;
        _customerRepository = customerRepository;
        _deliveryAssignmentService = deliveryAssignmentService;
    }

// DeliveryService.CreateAsync — reemplaza el método actual
public async Task<DeliveryResponseDto> CreateAsync(CreateDeliveryDto dto, int currentEmployeeId)
{
    var canAssign = await _authorizationService.CanPerformActionAsync(currentEmployeeId, PermissionNames.TomarEntrega);
    if (!canAssign)
        throw new SaborExpress.Shared.Exceptions.ConflictException("No tienes permiso para asignar entregas manualmente.");

    var order = await _orderRepository.GetByIdAsync(dto.OrderId)
        ?? throw new ArgumentException("El pedido no existe.");

    if (order.AddressId is null)
        throw new SaborExpress.Shared.Exceptions.ConflictException("Este pedido no tiene una dirección de entrega asociada.");

    await _validator.ValidateCreateAsync(dto, currentEmployeeId);

    if (!await _deliveryRepository.IsDeliveryPersonAsync(dto.DeliveryPersonId))
        throw new ArgumentException("El empleado indicado no es un repartidor activo.");

    var employeeBranchId = await _deliveryRepository.GetEmployeeBranchIdAsync(dto.DeliveryPersonId)
        ?? throw new ArgumentException("El repartidor indicado no existe o no tiene sede asignada.");

    if (employeeBranchId != order.BranchId)
        throw new SaborExpress.Shared.Exceptions.ConflictException("El repartidor debe pertenecer a la misma sede del pedido.");

    var delivery = new Delivery
    {
        OrderId = dto.OrderId,
        AddressId = order.AddressId.Value,
        DeliveryPersonId = dto.DeliveryPersonId,
        Status = DeliveryStatus.Assigned,
        AssignedAt = DateTime.UtcNow
    };

    await _deliveryRepository.AddAsync(delivery);
    await _deliveryRepository.SaveChangesAsync();
    await _realtime.DeliveriesChangedAsync();

    var created = await _deliveryRepository.GetByIdAsync(delivery.Id);
    return DeliveryMapper.ToResponse(created!);
}



        public async Task<List<DeliveryResponseDto>> GetByOrderIdAsync(int orderId)
        {
            var deliveries = await _deliveryRepository.GetByOrderIdAsync(orderId);
            return deliveries.Select(DeliveryMapper.ToResponse).ToList();
        }

        public async Task<CustomerDeliveryTrackingDto> GetCustomerTrackingAsync(int orderId, int customerId)
        {
            var deliveries = await _deliveryRepository.GetByOrderIdAsync(orderId);
            var delivery = deliveries.FirstOrDefault(d => d.Order?.CustomerId == customerId);
            if (delivery == null)
                throw new KeyNotFoundException("No se encontró el seguimiento de este pedido.");

            var now = DateTime.UtcNow;
            var employee = delivery.DeliveryPerson;
            var isLocationFresh = delivery.Status == DeliveryStatus.InTransit
                && employee?.CurrentLatitude != null
                && employee.CurrentLongitude != null
                && employee.LocationUpdatedAt >= now.AddSeconds(-45)
                && employee.LocationUpdatedAt <= now.AddSeconds(30);

            return new CustomerDeliveryTrackingDto
            {
                OrderId = delivery.OrderId,
                DeliveryId = delivery.Id,
                Status = delivery.Status,
                CourierLatitude = isLocationFresh ? employee!.CurrentLatitude : null,
                CourierLongitude = isLocationFresh ? employee!.CurrentLongitude : null,
                LocationUpdatedAt = isLocationFresh ? employee!.LocationUpdatedAt : null
            };
        }

        public async Task<List<DeliveryResponseDto>> GetByDeliveryPersonIdAsync(int deliveryPersonId, bool activeOnly = false)
        {
            var deliveries = await _deliveryRepository.GetByDeliveryPersonIdAsync(deliveryPersonId, activeOnly);
            return deliveries.Select(DeliveryMapper.ToResponse).ToList();
        }

        public async Task<List<DeliveryResponseDto>> GetByBranchIdAsync(int branchId)
        {
            var deliveries = await _deliveryRepository.GetByBranchIdAsync(branchId);
            return deliveries.Select(DeliveryMapper.ToResponse).ToList();
        }

        public async Task<DeliveryResponseDto> UpdateStatusAsync(
            int id,
            UpdateDeliveryStatusDto dto,
            int currentEmployeeId,
            bool isAdmin)
        {
            var delivery = await _deliveryRepository.GetByIdAsync(id);
            if (delivery == null)
                throw new ArgumentException("El domicilio no existe");

            if (!isAdmin)
            {
                var canUpdate = await _authorizationService.CanPerformActionAsync(currentEmployeeId, PermissionNames.ActualizarEstadoEntrega);
                if (!canUpdate)
                    throw new SaborExpress.Shared.Exceptions.ConflictException("No tienes permiso para actualizar entregas ahora mismo.");
            }

            _validator.ValidateStatusChange(delivery, dto, currentEmployeeId, isAdmin);

            // NUEVO: si el pedido fue cancelado, no se puede salir ni entregar
            if (delivery.Order.Status == OrderStatus.Cancelled
                && dto.Status != DeliveryStatus.Assigned)
                throw new SaborExpress.Shared.Exceptions.ConflictException(
                    "Este pedido fue cancelado. No puedes continuar con la entrega.");

            // El repartidor solo puede salir cuando cocina terminó todos los platos
            if (dto.Status == DeliveryStatus.InTransit)
            {
                var kitchenPending = delivery.Order.OrderDetails.Any(d =>
                    d.Status != OrderDetailStatus.Voided
                    && d.Status != OrderDetailStatus.Cancelled
                    && d.Status != OrderDetailStatus.Ready
                    && d.Status != OrderDetailStatus.Delivered);

                if (kitchenPending)
                    throw new SaborExpress.Shared.Exceptions.ConflictException(
                        "La cocina aún no termina este pedido. Espera a que esté listo para salir.");
            }

            var deliveredAt = dto.Status == DeliveryStatus.Delivered ? DateTime.UtcNow : (DateTime?)null;
            if (!await _deliveryRepository.TryTransitionStatusAsync(
                    id, delivery.Status, dto.Status, deliveredAt))
                throw new SaborExpress.Shared.Exceptions.ConflictException(
                    "El estado de este domicilio cambiÃ³ en otro dispositivo. Actualiza la lista e intÃ©ntalo de nuevo.");

            if (dto.Status == DeliveryStatus.Delivered)
            {
                await RegisterCashCollectionIfNeededAsync(delivery);
                await MarkOrderDeliveredAsync(delivery.OrderId);
            }

            await _deliveryRepository.SaveChangesAsync();

            // El cajero ve al instante que salió o se entregó (y el cobro en efectivo)
            await _realtime.OrdersChangedAsync();
            await _realtime.DeliveriesChangedAsync();
            await NotifyCustomerDeliveryProgressAsync(delivery);

            if (dto.Status == DeliveryStatus.Delivered)
            {
                try
                {
                    // El repartidor liberó un cupo; intenta asignar el siguiente pedido
                    // confirmado que siga esperando en la sede.
                    await _deliveryAssignmentService.AssignPendingForBranchAsync(delivery.Order.BranchId);
                }
                catch (Exception ex)
                {
                    _logger.LogError("No se pudieron reasignar pedidos pendientes tras entregar el pedido ({ErrorType}).", ex.GetType().Name);
                }
            }

            var updated = await _deliveryRepository.GetByIdAsync(delivery.Id);
            return DeliveryMapper.ToResponse(updated!);
        }

        // Avisa al cliente cuando el repartidor sale y cuando entrega.
        // Si falla, el cambio de estado ya quedó guardado.
        private async Task NotifyCustomerDeliveryProgressAsync(Delivery delivery)
        {
            try
            {
                if (delivery.Status != DeliveryStatus.InTransit
                    && delivery.Status != DeliveryStatus.Delivered)
                    return;

                var customerId = delivery.Order.CustomerId;
                if (!customerId.HasValue) return;

                var customerUserId = await _customerRepository.GetUserIdByCustomerIdAsync(customerId.Value);
                if (customerUserId == null) return;

                var inTransit = delivery.Status == DeliveryStatus.InTransit;

                await _notificationService.CreateAsync(
                    customerUserId.Value,
                    inTransit ? "Tu pedido va en camino" : "Pedido entregado",
                    inTransit
                        ? "Tu pedido ya salió hacia tu dirección."
                        : "Tu pedido fue entregado. ¡Buen provecho!",
                    NotificationType.OrderStatusChanged,
                    relatedEntityType: "Order",
                    relatedEntityId: delivery.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError("No se pudo notificar al cliente el avance del pedido ({ErrorType}).", ex.GetType().Name);
            }
        }

        // El cliente califica al repartidor una sola vez, cuando el pedido ya fue entregado.
        public async Task<DeliveryResponseDto> RateAsync(int id, RateDeliveryDto dto, int customerId)
        {
            if (dto.Score < 1 || dto.Score > 5)
                throw new ArgumentException("La calificación debe estar entre 1 y 5.");

            var delivery = await _deliveryRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("El domicilio no existe.");

            if (delivery.Order.CustomerId != customerId)
                throw new SaborExpress.Shared.Exceptions.ConflictException("Este pedido no te pertenece.");

            if (delivery.Status != DeliveryStatus.Delivered)
                throw new SaborExpress.Shared.Exceptions.ConflictException("Solo puedes calificar cuando el pedido ya fue entregado.");

            if (delivery.CustomerRating != null)
                throw new SaborExpress.Shared.Exceptions.ConflictException("Ya calificaste a este repartidor en este pedido.");

            var comment = dto.Comment?.Trim();
            if (!string.IsNullOrEmpty(comment) && comment.Length > 250)
                comment = comment[..250];

            delivery.CustomerRating = dto.Score;
            delivery.CustomerRatingComment = string.IsNullOrEmpty(comment) ? null : comment;
            delivery.RatedAt = DateTime.UtcNow;

            await _deliveryRepository.UpdateAsync(delivery);
            await _deliveryRepository.SaveChangesAsync();
            await _realtime.ReviewsChangedAsync();

            await NotifyRatingAsync(delivery, dto.Score, delivery.CustomerRatingComment);

            var updated = await _deliveryRepository.GetByIdAsync(delivery.Id);
            return DeliveryMapper.ToResponse(updated!);
        }
                // Avisa a Gerentes y Administradores de la sede. Si falla, la calificación igual queda guardada.
        private async Task NotifyRatingAsync(Delivery delivery, int score, string? comment)
        {
            try
            {
                var branchId = delivery.Order.BranchId;
                var recipients = await _notificationRepository.GetReviewRecipientUserIdsAsync(branchId);
                if (recipients.Count == 0) return;

                var customer = delivery.Order.Customer != null
                    ? $"{delivery.Order.Customer.Name} {delivery.Order.Customer.LastName}".Trim()
                    : (delivery.Order.GuestName ?? "Un cliente");

                var courier = delivery.DeliveryPerson != null
                    ? $"{delivery.DeliveryPerson.Name} {delivery.DeliveryPerson.LastName}".Trim()
                    : "Sin nombre";

                var message = $"Cliente: {customer}\nRepartidor: {courier}\nCalificación: {score}/5";
                if (!string.IsNullOrWhiteSpace(comment))
                    message += $"\nComentario: {comment}";

                await _notificationService.CreateBulkAsync(
                    recipients,
                    "Calificación de un pedido",
                    message,
                    NotificationType.ReviewReceived,
                    "Order",
                    delivery.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError("No se pudo notificar la calificación del pedido ({ErrorType}).", ex.GetType().Name);
            }
        }
                // Si al entregar el pedido aún tiene saldo sin pagar, se asume que el
        // repartidor lo cobró en efectivo y se registra el pago.
        private async Task RegisterCashCollectionIfNeededAsync(Delivery delivery)
        {
            var order = await _orderRepository.GetByIdAsync(delivery.OrderId);
            if (order == null) return;

            var payments = await _paymentRepository.GetByOrderIdAsync(order.Id);
            var alreadyPaid = payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            var pending = order.Total - alreadyPaid;
            if (pending <= 0) return;

            await _paymentRepository.TryAddCompletedPaymentAtomicallyAsync(new Payment
            {
                OrderId = order.Id,
                CashierId = delivery.DeliveryPersonId, // quién cobró el efectivo
                Method = PaymentMethod.Cash,
                Amount = pending,
                Status = PaymentStatus.Completed,
                PaidAt = DateTime.UtcNow
            });
        }
                // Al entregarse el domicilio, el pedido también queda como entregado.
        private async Task MarkOrderDeliveredAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null
                || order.Status == OrderStatus.Delivered
                || order.Status == OrderStatus.Cancelled)
                return;

            order.Status = OrderStatus.Delivered;
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);
        }
    }
}
