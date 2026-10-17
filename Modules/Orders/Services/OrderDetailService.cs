// Modules/Orders/Services/OrderDetailService.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Mappings;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Orders.Validators;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Notifications.Enum;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Enum;
using SaborExpress.Modules.Tables.Interfaces;
namespace SaborExpress.Modules.Orders.Services
{
    public class OrderDetailService : IOrderDetailService
    {
        private readonly IOrderDetailRepository _orderDetailRepository;
        private readonly IOrderDetailHistoryRepository _historyRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly OrderDetailValidator _validator;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUserNotificationService _notificationService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IRealtimeNotifier _realtime;
        private readonly IDeliveryRepository _deliveryRepository;
        private readonly ITableAvailabilityService _tableAvailabilityService;


        public OrderDetailService(
            IOrderDetailRepository orderDetailRepository,
            IOrderDetailHistoryRepository historyRepository,
            IOrderRepository orderRepository,
            IConfigurationRepository configurationRepository,
            OrderDetailValidator validator,
            IEmployeeRepository employeeRepository,
            IUserNotificationService notificationService,
            IPaymentRepository paymentRepository,
            IRealtimeNotifier realtime,
            IDeliveryRepository deliveryRepository,
            ITableAvailabilityService tableAvailabilityService)
        {
            _tableAvailabilityService = tableAvailabilityService;
            _deliveryRepository = deliveryRepository;
            _realtime = realtime;
            _paymentRepository = paymentRepository;
            _orderDetailRepository = orderDetailRepository;
            _historyRepository = historyRepository;
            _orderRepository = orderRepository;
            _configurationRepository = configurationRepository;
            _validator = validator;
            _employeeRepository = employeeRepository;
            _notificationService = notificationService;
        }

        public async Task<List<OrderDetailResponseDto>> GetByOrderIdAsync(int orderId)
        {
            var details = await _orderDetailRepository.GetByOrderIdAsync(orderId);
            return details.Select(OrderDetailMapper.ToResponse).ToList();
        }
                // Un pedido pagado por completo no admite más productos: al cobrar, la
        // mesa se libera, así que lo adicional debe ir en un pedido nuevo.
        // Sin esto el pedido "revive" y cocina/cajero ven lo ya pagado otra vez.
        private async Task EnsureOrderNotFullyPaidAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.Total <= 0)
                return;

            var payments = await _paymentRepository.GetByOrderIdAsync(orderId);
            var totalPaid = payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            if (totalPaid >= order.Total)
                throw new InvalidOperationException(
                    "Este pedido ya fue pagado por completo. Crea un pedido nuevo para los productos adicionales.");
        }

        public async Task<OrderDetailResponseDto> CreateAsync(int orderId, CreateOrderDetailDto dto, int employeeId)
        {
            await EnsureOrderNotFullyPaidAsync(orderId);

            // El validator ya trajo y valido el producto; no hace falta pedirlo de nuevo
            var product = await _validator.ValidateCreateAsync(orderId, dto);

            var orderDetail = new OrderDetail
            {
                OrderId = orderId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                Notes = dto.Notes,
                IsToGo = dto.IsToGo,
                UnitPrice = product.Price,
                SubTotal = product.Price * dto.Quantity,
                LastModifiedByEmployeeId = employeeId,
                Status = OrderDetailStatus.Pending
            };

            await _orderDetailRepository.AddAsync(orderDetail);
            await _orderDetailRepository.SaveChangesAsync();

            await _historyRepository.AddAsync(new OrderDetailHistory
            {
                OrderDetailId = orderDetail.Id,
                ChangedByEmployeeId = employeeId,
                Action = OrderDetailHistoryAction.Created,
                NewValue = $"Qty: {orderDetail.Quantity}, Notes: {orderDetail.Notes}",
                ChangedAt = DateTime.UtcNow
            });
            await _historyRepository.SaveChangesAsync();

            await _orderRepository.RecalculateTotalsAsync(orderId);
            await _realtime.OrdersChangedAsync();

            var created = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(created!);
        }

        public async Task<List<OrderDetailResponseDto>> CreateBatchAsync(int orderId, CreateOrderDetailsBatchDto dto, int? employeeId, int? customerId = null)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new ArgumentException("Debe incluir al menos un producto en el pedido.");

            // Un cliente solo puede agregar productos a SU propio pedido
            if (customerId.HasValue)
            {
                var ownerOrder = await _orderRepository.GetByIdAsync(orderId)
                    ?? throw new ArgumentException("El pedido no existe");
                if (ownerOrder.CustomerId != customerId.Value)
                    throw new InvalidOperationException("Este pedido no te pertenece.");
            }

            await EnsureOrderNotFullyPaidAsync(orderId);

            var nextBatch = await _orderDetailRepository.GetNextBatchNumberAsync(orderId);
            var created = new List<OrderDetail>();

            foreach (var itemDto in dto.Items)
            {
                var product = await _validator.ValidateCreateAsync(orderId, itemDto);

                var orderDetail = new OrderDetail
                {
                    OrderId = orderId,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    Notes = itemDto.Notes,
                    IsToGo = itemDto.IsToGo,
                    BatchNumber = nextBatch,
                    UnitPrice = product.Price,
                    SubTotal = product.Price * itemDto.Quantity,
                    LastModifiedByEmployeeId = employeeId,
                    Status = OrderDetailStatus.Pending
                };

                await _orderDetailRepository.AddAsync(orderDetail);
                created.Add(orderDetail);
            }

            await _orderDetailRepository.SaveChangesAsync();

            foreach (var orderDetail in created)
            {
                await _historyRepository.AddAsync(new OrderDetailHistory
                {
                    OrderDetailId = orderDetail.Id,
                    ChangedByEmployeeId = employeeId,
                    Action = OrderDetailHistoryAction.Created,
                    NewValue = $"Qty: {orderDetail.Quantity}, Lote: {orderDetail.BatchNumber}",
                    ChangedAt = DateTime.UtcNow
                });
            }
            await _historyRepository.SaveChangesAsync();

            await _orderRepository.RecalculateTotalsAsync(orderId);

            await _realtime.OrdersChangedAsync();

            // NUEVO: sin esto, cocina solo se enteraba del extra por el
            // polling de 15s (sin sonido), y el mesero no se enteraba NUNCA
            // — su pantalla de Órdenes Activas no refresca sola.
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order != null)
                await NotifyBatchAddedAsync(order, employeeId);

            var result = new List<OrderDetailResponseDto>();
            foreach (var orderDetail in created)
            {
                var full = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
                result.Add(OrderDetailMapper.ToResponse(full!));
            }
            return result;
        }

        // Avisa a Cocina (y Admin/Gerente de la sede) que hay productos
        // nuevos que preparar — mismo tratamiento que un pedido nuevo, para
        // que suene igual en el tablero de cocina. También avisa al Mesero
        // dueño del pedido (si fue el cajero quien agregó el extra), para
        // que su lista de Órdenes Activas se actualice sola.
        private async Task NotifyBatchAddedAsync(Order order, int? addedByEmployeeId)
        {
            // Domicilio de la app: se avisa cuando el pago queda aprobado,
            // no cuando el cliente apenas arma el carrito.
            if (order.OrderType == OrderType.Delivery && order.Channel == OrderChannel.App)
                return;

            var cocineroIds = await _employeeRepository.GetUserIdsByBranchAndRolesAsync(
                order.BranchId, RoleNames.Cocinero);
            var adminIds = await _employeeRepository.GetUserIdsByBranchAndRolesAsync(
                order.BranchId, RoleNames.Administrador);
            var gerenteIds = await _employeeRepository.GetUserIdsByRolesAsync(RoleNames.Gerente);

            var kitchenUserIds = cocineroIds.Concat(adminIds).Concat(gerenteIds).Distinct().ToList();
            if (kitchenUserIds.Count > 0)
            {
                await _notificationService.CreateBulkAsync(
                    kitchenUserIds,
                    "Pedido actualizado",
                    "Se agregaron productos al pedido.",
                    NotificationType.OrderCreated,
                    relatedEntityType: "Order",
                    relatedEntityId: order.Id);
            }

            // Si lo agregó un cliente (sin empleado), adderUserId queda null
            // y se avisa a todos los cajeros de la sede.
            int? adderUserId = null;
            if (addedByEmployeeId.HasValue)
                adderUserId = (await _employeeRepository.GetByIdAsync(addedByEmployeeId.Value))?.UserId;

            var cajeroIds = await _employeeRepository.GetUserIdsByBranchAndRolesAsync(
                order.BranchId, RoleNames.Cajero);

            var notifiedCashierIds = cajeroIds.Distinct().Where(id => id != adderUserId).ToList();
            if (notifiedCashierIds.Count > 0)
            {
                await _notificationService.CreateBulkAsync(
                    notifiedCashierIds,
                    "Pedido nuevo",
                    "Hay productos nuevos para cobrar.",
                    NotificationType.OrderCreated,
                    relatedEntityType: "Order",
                    relatedEntityId: order.Id);
            }

            if (order.EmployeeId.HasValue && order.EmployeeId != addedByEmployeeId)
            {
                var mesero = await _employeeRepository.GetByIdAsync(order.EmployeeId.Value);
                if (mesero != null)
                {
                    await _notificationService.CreateAsync(
                        mesero.UserId,
                        "Pedido actualizado",
                        "Se agregaron productos al pedido.",
                        NotificationType.OrderStatusChanged,
                        relatedEntityType: "Order",
                        relatedEntityId: order.Id);
                }
            }
        }

        public async Task<List<OrderDetailResponseDto>> UpdateBatchStatusAsync(int orderId, int batchNumber, UpdateBatchStatusDto dto, int employeeId)
        {
            if (dto.Status == OrderDetailStatus.Delivered)
                throw new InvalidOperationException(
                    "La entrega al mesero se debe confirmar desde la acción de entrega del pedido.");

            var order = await _orderRepository.GetByIdAsync(orderId)
                ?? throw new ArgumentException("El pedido no existe.");

            if (order.Channel == OrderChannel.App && order.Status == OrderStatus.Pending)
                throw new InvalidOperationException(
                    "El pedido del cliente está pendiente de confirmación en caja. Cocina podrá iniciarlo cuando quede confirmado.");

            var items = await _orderDetailRepository.GetByOrderAndBatchAsync(orderId, batchNumber);
            if (items.Count == 0)
                throw new ArgumentException("El lote indicado no existe para este pedido.");

            foreach (var item in items)
            {
                if (item.Status == OrderDetailStatus.Voided || item.Status == OrderDetailStatus.Cancelled)
                    continue; // no reactivamos platos anulados/cancelados al avanzar el lote

                var oldStatus = item.Status;
                item.Status = dto.Status;
                item.LastModifiedByEmployeeId = employeeId;

                await _orderDetailRepository.UpdateAsync(item);

                await _historyRepository.AddAsync(new OrderDetailHistory
                {
                    OrderDetailId = item.Id,
                    ChangedByEmployeeId = employeeId,
                    Action = OrderDetailHistoryAction.Updated,
                    OldValue = oldStatus.ToString(),
                    NewValue = dto.Status.ToString(),
                    ChangedAt = DateTime.UtcNow
                });
            }

            await _orderDetailRepository.SaveChangesAsync();
            await _historyRepository.SaveChangesAsync();

            // NUEVO: sincronizamos el estado general del pedido con el estado
            // agregado de sus platos, y avisamos al Mesero dueño del pedido.
            await SyncOrderStatusAndNotifyAsync(orderId, batchNumber, dto.Status);
            await _realtime.OrdersChangedAsync();

            var result = new List<OrderDetailResponseDto>();
            foreach (var item in items)
            {
                var full = await _orderDetailRepository.GetByIdAsync(item.Id);
                result.Add(OrderDetailMapper.ToResponse(full!));
            }
            return result;
        }

        // Recalcula Order.Status a partir del estado agregado de todos sus
        // platos vivos (ignora Voided/Cancelled), y notifica al Mesero dueño
        // del pedido con la mesa y el nuevo estado del lote que cambió.
        private async Task SyncOrderStatusAndNotifyAsync(int orderId, int batchNumber, OrderDetailStatus newBatchStatus)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                return;

            var allItems = await _orderDetailRepository.GetByOrderIdAsync(orderId);
            var liveItems = allItems
                .Where(i => i.Status != OrderDetailStatus.Voided && i.Status != OrderDetailStatus.Cancelled)
                .ToList();

            var orderBecameReady = false;

            if (liveItems.Count > 0)
            {
                OrderStatus? newOrderStatus = null;

                if (liveItems.All(i => i.Status == OrderDetailStatus.Ready))
                    newOrderStatus = OrderStatus.Ready;
                else if (liveItems.Any(i => i.Status == OrderDetailStatus.InPreparation || i.Status == OrderDetailStatus.Ready))
                    newOrderStatus = OrderStatus.InPreparation;

                // Solo avanzamos hacia adelante, y solo si el pedido sigue
                // "vivo" (no tocamos Delivered/Cancelled).
                if (newOrderStatus.HasValue
                    && order.Status != OrderStatus.Delivered
                    && order.Status != OrderStatus.Cancelled
                    && order.Status != newOrderStatus.Value
                    && (int)newOrderStatus.Value > (int)order.Status)
                {
                    order.Status = newOrderStatus.Value;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateAsync(order);
                    await _orderRepository.SaveChangesAsync();

                    orderBecameReady = newOrderStatus.Value == OrderStatus.Ready;
                }
            }

            // NUEVO: cuando TODO el pedido queda listo, avisamos a los Cajeros de
            // la sede (para que suene y refresquen su lista). Si el dueño del
            // pedido también es Cajero, ya recibe el aviso de Mesero: no se duplica.
            if (orderBecameReady)
            {
                int? meseroUserId = null;
                if (order.EmployeeId.HasValue)
                    meseroUserId = (await _employeeRepository.GetByIdAsync(order.EmployeeId.Value))?.UserId;

                var cajeroIds = await _employeeRepository.GetUserIdsByBranchAndRolesAsync(
                    order.BranchId, RoleNames.Cajero);

                foreach (var cajeroUserId in cajeroIds.Distinct().Where(id => id != meseroUserId))
                {
                    await _notificationService.CreateAsync(
                        cajeroUserId,
                        "Pedido listo",
                        "Un pedido está listo para cobrar.",
                        NotificationType.OrderReady,
                        relatedEntityType: "Order",
                        relatedEntityId: order.Id);
                }

                // Domicilio: avisa al repartidor asignado que ya puede salir
                if (order.OrderType == OrderType.Delivery)
                {
                    try
                    {
                        var deliveries = await _deliveryRepository.GetByOrderIdAsync(order.Id);
                        foreach (var d in deliveries.Where(d => d.Status != DeliveryStatus.Delivered))
                        {
                            var driver = await _employeeRepository.GetByIdAsync(d.DeliveryPersonId);
                            if (driver == null) continue;

                            await _notificationService.CreateAsync(
                                driver.UserId,
                                "Pedido listo para recoger",
                                "Un pedido ya está listo. Puedes iniciar la entrega.",
                                NotificationType.OrderReady,
                                relatedEntityType: "Order",
                                relatedEntityId: order.Id);
                        }
                    }
                    catch
                    {
                        // El estado ya quedó guardado; un fallo al notificar no debe romperlo.
                    }
                }
            }

            // Solo notificamos al Mesero cuando el lote queda LISTO.
            // "En preparación" ya no genera notificación: el mesero solo
            // quiere enterarse cuando el plato está listo para servir.
            if (newBatchStatus == OrderDetailStatus.Ready && order.EmployeeId.HasValue)
            {
                var mesero = await _employeeRepository.GetByIdAsync(order.EmployeeId.Value);
                if (mesero != null)
                {
                    var tableLabel = order.TableId.HasValue
                        ? $"Mesa {order.Table?.Number}"
                        : (order.GuestName ?? "Para llevar");

                    await _notificationService.CreateAsync(
                        mesero.UserId,
                        "Pedido listo",
                        $"{tableLabel}: el pedido está listo para servir.",
                        NotificationType.OrderReady,
                        relatedEntityType: "Order",
                        relatedEntityId: order.Id);
                }
            }
        }

        public async Task<OrderDetailResponseDto> UpdateAsync(int id, UpdateOrderDetailDto dto, int employeeId)
        {
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("La linea del pedido no existe");

            await _validator.ValidateUpdate(orderDetail, dto);

            var oldValue = $"Qty: {orderDetail.Quantity}, Notes: {orderDetail.Notes}";

            orderDetail.Quantity = dto.Quantity;
            orderDetail.Notes = dto.Notes;
            orderDetail.SubTotal = orderDetail.UnitPrice * dto.Quantity;
            orderDetail.LastModifiedByEmployeeId = employeeId;

            await _orderDetailRepository.UpdateAsync(orderDetail);
            await _orderDetailRepository.SaveChangesAsync();

            await _historyRepository.AddAsync(new OrderDetailHistory
            {
                OrderDetailId = orderDetail.Id,
                ChangedByEmployeeId = employeeId,
                Action = OrderDetailHistoryAction.Updated,
                OldValue = oldValue,
                NewValue = $"Qty: {orderDetail.Quantity}, Notes: {orderDetail.Notes}",
                ChangedAt = DateTime.UtcNow
            });
            await _historyRepository.SaveChangesAsync();

            await _orderRepository.RecalculateTotalsAsync(orderDetail.OrderId);

            var updated = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(updated!);
        }

        public async Task<OrderDetailResponseDto> UpdateToGoAsync(
            int id,
            UpdateOrderDetailToGoDto dto,
            int employeeId)
        {
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("La linea del pedido no existe");

            if (orderDetail.Status == OrderDetailStatus.Voided ||
                orderDetail.Status == OrderDetailStatus.Cancelled)
            {
                throw new ArgumentException(
                    "No se puede cambiar el tipo de una linea anulada o cancelada");
            }

            if (orderDetail.IsToGo == dto.IsToGo)
                return OrderDetailMapper.ToResponse(orderDetail);

            var oldValue = orderDetail.IsToGo ? "Para llevar" : "Mesa";
            var newValue = dto.IsToGo ? "Para llevar" : "Mesa";
            orderDetail.IsToGo = dto.IsToGo;
            orderDetail.LastModifiedByEmployeeId = employeeId;

            await _orderDetailRepository.UpdateAsync(orderDetail);
            await _orderDetailRepository.SaveChangesAsync();

            await _historyRepository.AddAsync(new OrderDetailHistory
            {
                OrderDetailId = orderDetail.Id,
                ChangedByEmployeeId = employeeId,
                Action = OrderDetailHistoryAction.Updated,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedAt = DateTime.UtcNow,
            });
            await _historyRepository.SaveChangesAsync();
            await _realtime.OrdersChangedAsync();

            var updated = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(updated!);
        }

        public async Task<OrderDetailResponseDto> VoidAsync(int id, VoidOrderDetailDto dto, int employeeId)
        {
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("La linea del pedido no existe");

            await _validator.ValidateVoid(orderDetail, dto);

            // Anular el último producto NO cancela el pedido ni libera la mesa.
            // Obligar a usar CancelAsync preserva el historial y las reglas
            // de pagos y de cocina también para otros clientes de esta API.
            var liveDetails = (await _orderDetailRepository.GetByOrderIdAsync(orderDetail.OrderId))
                .Where(detail => detail.Status != OrderDetailStatus.Voided &&
                                 detail.Status != OrderDetailStatus.Cancelled)
                .ToList();
            if (liveDetails.Count == 1 && liveDetails[0].Id == orderDetail.Id)
                throw new InvalidOperationException(
                    "Para quitar el último producto debes cancelar el pedido completo.");

            var oldStatus = orderDetail.Status;
            orderDetail.Status = OrderDetailStatus.Voided;
            orderDetail.LastModifiedByEmployeeId = employeeId;

            await _orderDetailRepository.UpdateAsync(orderDetail);
            await _orderDetailRepository.SaveChangesAsync();

            await _historyRepository.AddAsync(new OrderDetailHistory
            {
                OrderDetailId = orderDetail.Id,
                ChangedByEmployeeId = employeeId,
                Action = OrderDetailHistoryAction.Voided,
                OldValue = oldStatus.ToString(),
                NewValue = OrderDetailStatus.Voided.ToString(),
                Reason = dto.Reason,
                ChangedAt = DateTime.UtcNow
            });
            await _historyRepository.SaveChangesAsync();

            await _orderRepository.RecalculateTotalsAsync(orderDetail.OrderId);
            await _realtime.OrdersChangedAsync();

            var voided = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(voided!);
        }
        
        public async Task<OrderDetailResponseDto> MarkDeliveredAsync(int id, int employeeId)
        {
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("La linea del pedido no existe");

            EnsureCanBeDelivered(orderDetail);
            if (orderDetail.Status != OrderDetailStatus.Ready)
                throw new InvalidOperationException(
                    "Solo puedes marcar como entregado un producto que cocina ya marc\u00f3 como listo.");

            var oldStatus = orderDetail.Status;
            orderDetail.Status = OrderDetailStatus.Delivered;
            orderDetail.LastModifiedByEmployeeId = employeeId;

            await _orderDetailRepository.UpdateAsync(orderDetail);
            await _orderDetailRepository.SaveChangesAsync();

            await _historyRepository.AddAsync(new OrderDetailHistory
            {
                OrderDetailId = orderDetail.Id,
                ChangedByEmployeeId = employeeId,
                Action = OrderDetailHistoryAction.Updated, // ⚠️ ver nota abajo
                OldValue = oldStatus.ToString(),
                NewValue = OrderDetailStatus.Delivered.ToString(),
                ChangedAt = DateTime.UtcNow
            });

            await _historyRepository.SaveChangesAsync();

            var details = await _orderDetailRepository
                .GetByOrderIdAsync(orderDetail.OrderId);

            var activeDetails = details
                .Where(detail =>
                    detail.Status != OrderDetailStatus.Cancelled &&
                    detail.Status != OrderDetailStatus.Voided)
                .ToList();

            var allDelivered =
                activeDetails.Count > 0 &&
                activeDetails.All(detail =>
                    detail.Status == OrderDetailStatus.Delivered);

            if (allDelivered)
            {
                var order = await _orderRepository.GetByIdAsync(orderDetail.OrderId);

                if (order != null &&
                    order.Status != OrderStatus.Cancelled &&
                    order.Status != OrderStatus.Delivered)
                {
                    order.Status = OrderStatus.Delivered;
                    order.UpdatedAt = DateTime.UtcNow;

                    await _orderRepository.UpdateAsync(order);
                    await _orderRepository.SaveChangesAsync();
                }
            }

            await _tableAvailabilityService
                .TryReleaseAfterConfirmationAsync(orderDetail.OrderId);

            await _realtime.OrdersChangedAsync();


            var delivered = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(delivered!);
        }

        private static void EnsureCanBeDelivered(OrderDetail orderDetail)
        {
            if (orderDetail.Status == OrderDetailStatus.Voided)
                throw new InvalidOperationException("No puedes entregar una línea que fue anulada.");

            if (orderDetail.Status == OrderDetailStatus.Cancelled)
                throw new InvalidOperationException("No puedes entregar una línea que fue cancelada.");

            if (orderDetail.Status == OrderDetailStatus.Delivered)
                throw new InvalidOperationException("Esta línea ya fue marcada como entregada.");
        }
    }
}
