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
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Enum;
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

        // Usado solo si la sede nunca configuró TaxRate en Configuraciones.
        private const decimal DefaultTaxRate = 0.19m;

        public OrderDetailService(
            IOrderDetailRepository orderDetailRepository,
            IOrderDetailHistoryRepository historyRepository,
            IOrderRepository orderRepository,
            IConfigurationRepository configurationRepository,
            OrderDetailValidator validator,
            IEmployeeRepository employeeRepository,
            IUserNotificationService notificationService,
            IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
            _orderDetailRepository = orderDetailRepository;
            _historyRepository = historyRepository;
            _orderRepository = orderRepository;
            _configurationRepository = configurationRepository;
            _validator = validator;
            _employeeRepository = employeeRepository;
            _notificationService = notificationService;
        }

        // Lee TaxRate de la sede del pedido. Si esa sede nunca lo configuró
        // (fila inexistente en BranchSetting) o el valor guardado no es un
        // número válido, cae al 19% por defecto en vez de romper el pedido.
        private async Task<decimal> GetTaxRateAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                return DefaultTaxRate;

            var setting = await _configurationRepository.GetByKeyAsync(order.BranchId, "TaxRate");
            if (setting == null)
                return DefaultTaxRate;

            // El valor viene como texto ("19" o "19.5"); se fuerza cultura
            // invariante (punto decimal) para que no dependa de la config
            // regional del servidor. TaxRate se guarda como porcentaje
            // (19 = 19%), así que se divide entre 100 al usarlo.
            if (!decimal.TryParse(
                    setting.Value,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var percent))
                return DefaultTaxRate;

            return percent / 100m;
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

            await _orderRepository.RecalculateTotalsAsync(orderId, await GetTaxRateAsync(orderId));

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

            await _orderRepository.RecalculateTotalsAsync(orderId, await GetTaxRateAsync(orderId));

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

            var kitchenUserIds = cocineroIds.Concat(adminIds).Concat(gerenteIds).Distinct();

            foreach (var userId in kitchenUserIds)
            {
                await _notificationService.CreateAsync(
                    userId,
                    "Pedido actualizado",
                    $"Se agregaron productos al pedido #{order.Id}.",
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

            foreach (var cajeroUserId in cajeroIds.Distinct().Where(id => id != adderUserId))
            {
                await _notificationService.CreateAsync(
                    cajeroUserId,
                    "Pedido nuevo",
                    $"Pedido #{order.Id}: hay productos nuevos para cobrar.",
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
                        $"Se agregaron productos al pedido #{order.Id}.",
                        NotificationType.OrderStatusChanged,
                        relatedEntityType: "Order",
                        relatedEntityId: order.Id);
                }
            }
        }

        public async Task<List<OrderDetailResponseDto>> UpdateBatchStatusAsync(int orderId, int batchNumber, UpdateBatchStatusDto dto, int employeeId)
        {
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
                        $"El pedido #{order.Id} está listo para cobrar.",
                        NotificationType.OrderReady,
                        relatedEntityType: "Order",
                        relatedEntityId: order.Id);
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

            await _orderRepository.RecalculateTotalsAsync(orderDetail.OrderId, await GetTaxRateAsync(orderDetail.OrderId));

            var updated = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(updated!);
        }

        public async Task<OrderDetailResponseDto> VoidAsync(int id, VoidOrderDetailDto dto, int employeeId)
        {
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("La linea del pedido no existe");

            await _validator.ValidateVoid(orderDetail, dto);

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

            await _orderRepository.RecalculateTotalsAsync(orderDetail.OrderId, await GetTaxRateAsync(orderDetail.OrderId));

            var voided = await _orderDetailRepository.GetByIdAsync(orderDetail.Id);
            return OrderDetailMapper.ToResponse(voided!);
        }
        
        public async Task<OrderDetailResponseDto> MarkDeliveredAsync(int id, int employeeId)
        {
            var orderDetail = await _orderDetailRepository.GetByIdAsync(id)
                ?? throw new ArgumentException("La linea del pedido no existe");

            EnsureCanBeDelivered(orderDetail);

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