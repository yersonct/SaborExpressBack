using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Mappings;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Orders.Validators;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Modules.Tables.Enum;
using SaborExpress.Modules.Tables.Models;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Modules.Addresses.Interfaces;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Notifications.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Deliveries.Enum;

namespace SaborExpress.Modules.Orders.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderDetailRepository _orderDetailRepository;
        private readonly IOrderStatusHistoryRepository _statusHistoryRepository;
        private readonly ITableRepository _tableRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IBranchService _branchService; // NUEVO — para FindNearestBranchAsync
        private readonly IAddressRepository _addressRepository; // NUEVO
        private readonly IDeliveryAssignmentService _deliveryAssignmentService; // NUEVO
        private readonly IEmployeeScheduleRepository _scheduleRepository;
        private readonly OrderValidator _validator;
        private readonly IAuthorizationService _authorizationService;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUserNotificationService _notificationService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IDeliveryRepository _deliveryRepository;

        public OrderService(
            IOrderRepository orderRepository,
            IOrderDetailRepository orderDetailRepository,
            IOrderStatusHistoryRepository statusHistoryRepository,
            ITableRepository tableRepository,
            IBranchRepository branchRepository,
            IBranchService branchService, // NUEVO
            IAddressRepository addressRepository, // NUEVO
            IDeliveryAssignmentService deliveryAssignmentService, // NUEVO
            IEmployeeScheduleRepository scheduleRepository,
            OrderValidator validator,
            IAuthorizationService authorizationService,
            IConfigurationRepository configurationRepository,
            IAuthRepository authRepository,
            IEmployeeRepository employeeRepository,
            IUserNotificationService notificationService,
            IPaymentRepository paymentRepository,
            IDeliveryRepository deliveryRepository)
        {
            _orderRepository = orderRepository;
            _orderDetailRepository = orderDetailRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _tableRepository = tableRepository;
            _branchRepository = branchRepository;
            _branchService = branchService;
            _addressRepository = addressRepository;
            _deliveryAssignmentService = deliveryAssignmentService;
            _scheduleRepository = scheduleRepository;
            _validator = validator;
            _authorizationService = authorizationService;
            _configurationRepository = configurationRepository;
            _authRepository = authRepository;
            _employeeRepository = employeeRepository;
            _notificationService = notificationService;
            _paymentRepository = paymentRepository;
            _deliveryRepository = deliveryRepository;
        }
        public async Task<OrderResponseDto> CreateAsync(CreateOrderDto dto, int? employeeId, bool isClienteChannel)
        {
        var resolvedBranchId = await ResolveBranchIdAsync(dto, employeeId, isClienteChannel);

        await EnsureBranchIsOpenAsync(resolvedBranchId);

        await _validator.ValidateCreateAsync(dto, employeeId, resolvedBranchId);

        Table? table = null;
        if (dto.TableId.HasValue)
        {
            table = await _tableRepository.GetByIdAsync(dto.TableId.Value)
                ?? throw new ArgumentException("La mesa no existe.");

            if (table.BranchId != resolvedBranchId)
                throw new InvalidOperationException("La mesa no pertenece a la sede indicada.");

            if (table.Status != TableStatus.Available)
                throw new InvalidOperationException("La mesa ya no está disponible. Puede que otro mesero ya la haya tomado.");
        }

        var order = new Order
        {
            CustomerId = dto.CustomerId,
            EmployeeId = employeeId,
            TableId = dto.TableId,
            BranchId = resolvedBranchId,
            AddressId = dto.AddressId,
            OrderType = dto.OrderType,
            Channel = isClienteChannel ? OrderChannel.App : OrderChannel.Mostrador,
            Notes = dto.Notes,
            GuestName = dto.GuestName,
            Status = OrderStatus.Pending,
            SubTotal = 0,
            Tax = 0,
            Total = 0
        };

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        if (table != null)
        {
            table.Status = TableStatus.Occupied;
            await _tableRepository.UpdateAsync(table);
            await _tableRepository.SaveChangesAsync();
        }

        await _statusHistoryRepository.AddAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            ChangedByEmployeeId = employeeId,
            Status = OrderStatus.Pending,
            Notes = isClienteChannel ? "Pedido creado por el cliente desde la app" : "Pedido creado",
            ChangedAt = DateTime.UtcNow
        });
        await _statusHistoryRepository.SaveChangesAsync();

        await NotifyNewOrderAsync(order);

        var created = await _orderRepository.GetByIdAsync(order.Id);
        return OrderMapper.ToResponse(created!);
    }

    // Avisa a Administrador/Gerente (siempre) y a Cocinero (para el sonido en
    // el tablero de cocina). Auxiliar de Cocina NO se incluye a propósito: ve
    // el mismo tablero via polling, pero sin sonido, ya que no es quien actúa
    // sobre el pedido.
    private async Task NotifyNewOrderAsync(Order order)
    {
        // Domicilio hecho desde la app: aún no se sabe si el cliente va a pagar.
        // Se avisa cuando Wompi aprueba el pago (PaymentService).
        if (order.OrderType == OrderType.Delivery && order.Channel == OrderChannel.App)
            return;

        // Administrador: solo de su propia sede. Gerente: de todas las sedes.
        var adminIds = await _employeeRepository.GetUserIdsByBranchAndRolesAsync(
            order.BranchId, RoleNames.Administrador);
        var gerenteIds = await _employeeRepository.GetUserIdsByRolesAsync(RoleNames.Gerente);
        var cocineroIds = await _employeeRepository.GetUserIdsByBranchAndRolesAsync(
            order.BranchId, RoleNames.Cocinero);

        var userIds = adminIds.Concat(gerenteIds).Concat(cocineroIds).Distinct();

        foreach (var userId in userIds)
        {
            await _notificationService.CreateAsync(
                userId,
                "Pedido nuevo",
                $"Llegó el pedido #{order.Id}.",
                NotificationType.OrderCreated,
                relatedEntityType: "Order",
                relatedEntityId: order.Id);
        }
    }

// NUEVO
private async Task<int> ResolveBranchIdAsync(CreateOrderDto dto, int? employeeId, bool isClienteChannel)
{
    if (dto.OrderType != OrderType.Delivery)
    {
        // Mesa / Para Llevar los crea siempre un empleado (Mesero/Cajero).
        // Usamos la sede REAL y ACTUAL del empleado en la base de datos,
        // nunca el branchId que mande el móvil (puede venir de un token
        // desactualizado si el turno del empleado cambió después del login).
        if (!isClienteChannel && employeeId.HasValue)
        {
            var employee = await _employeeRepository.GetByIdAsync(employeeId.Value)
                ?? throw new KeyNotFoundException("El empleado no existe.");

            if (!employee.BranchId.HasValue)
                throw new InvalidOperationException(
                    "Tu cuenta no tiene una sede asignada todavía. Pide a tu gerente/administrador " +
                    "que te asigne un turno, y vuelve a iniciar sesión.");

            return employee.BranchId.Value;
        }

        // Sin empleado identificado (caso raro/edge): exige el branchId explícito.
        if (!dto.BranchId.HasValue || dto.BranchId.Value <= 0)
            throw new ArgumentException("Debe indicar una sucursal válida.");

        return dto.BranchId.Value;
    }

    // Delivery: el cliente manda AddressId, la sede se calcula sola.
    if (!dto.AddressId.HasValue)
        throw new ArgumentException("Un pedido a domicilio debe indicar una dirección.");

    var address = await _addressRepository.GetByIdAsync(dto.AddressId.Value)
        ?? throw new ArgumentException("La dirección indicada no existe.");

    if (dto.CustomerId.HasValue && address.CustomerId != dto.CustomerId.Value)
        throw new ArgumentException("La dirección no pertenece a este cliente.");

    var nearestBranch = await _branchService.FindNearestBranchAsync(address.Latitude, address.Longitude);
    return nearestBranch.Id;
}
        public async Task<OrderResponseDto> GetByIdAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            return OrderMapper.ToResponse(order);
        }


        public async Task<List<OrderSummaryDto>> GetAllAsync(OrderFilterDto filter, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var effectiveBranchId = await ResolveAllowedBranchIdAsync(filter.BranchId, currentUserId);

            int? effectiveEmployeeId = null;
            var esGerenteOAdmin = currentUser.HasRole(RoleNames.Gerente) || currentUser.HasRole(RoleNames.Administrador);

            // Solo se restringe a "mis propios pedidos" si el único rol operativo real
            // es Mesero. Si además es Cocinero, Cajero o Auxiliar de Cocina, esos roles
            // SÍ necesitan ver todos los pedidos de la sede (no solo los propios que él
            // haya tomado como mesero), así que no se filtra.
            var esSoloMesero = currentUser.HasRole(RoleNames.Mesero)
                && !currentUser.HasRole(RoleNames.Cocinero)
                && !currentUser.HasRole(RoleNames.Cajero)
                && !currentUser.HasRole(RoleNames.AuxiliarCocina);

            if (!esGerenteOAdmin && esSoloMesero)
            {
                effectiveEmployeeId = currentUser.Employee?.Id
                    ?? throw new InvalidOperationException("El usuario actual no tiene un perfil de empleado asociado.");
            }

            var orders = await _orderRepository.GetAllAsync(effectiveBranchId, filter.Status, effectiveEmployeeId);
            return orders.Select(OrderMapper.ToSummary).ToList();
        }

        public async Task<List<OrderSummaryDto>> GetByTableIdAsync(int tableId)
        {
            var orders = await _orderRepository.GetByTableIdAsync(tableId);
            return orders.Select(OrderMapper.ToSummary).ToList();
        }

        public async Task<List<OrderSummaryDto>> GetByCustomerIdAsync(int customerId)
        {
            var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
            return orders.Select(OrderMapper.ToSummary).ToList();
        }

        // CAMBIADO: recibe currentUserId, valida que la sede pedida sea la propia (salvo Gerente)
        public async Task<List<OrderSummaryDto>> GetByBranchIdAsync(int branchId, int currentUserId)
        {
            await EnsureCanAccessBranchAsync(branchId, currentUserId);

            var orders = await _orderRepository.GetByBranchIdAsync(branchId);
            return orders.Select(OrderMapper.ToSummary).ToList();
        }

        // Sin autenticación: pensado para un monitor de cocina fijo (tablet/TV) que
        // solo MUESTRA pedidos, sin login. El código público NO es el Id real de la
        // sede, para no exponerlo en una URL sin protección.
        public async Task<List<OrderSummaryDto>> GetKitchenBoardByAccessTokenAsync(string accessToken)
        {
            var schedule = await _scheduleRepository.GetActiveScheduleByTokenAsync(accessToken, DateTime.Now)
                ?? throw new UnauthorizedAccessException("Este link ya no es válido o el turno ha terminado.");

            var orders = await _orderRepository.GetByBranchIdAsync(schedule.BranchId);
            return orders.Select(OrderMapper.ToSummary).ToList();
        }

        public async Task<OrderResponseDto> UpdateAsync(int id, UpdateOrderDto dto, int employeeId)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            await EnsureCanEditOrderAsync(order, employeeId);

            await _validator.ValidateUpdateAsync(order, dto);

            order.TableId = dto.TableId;
            order.OrderType = dto.OrderType;
            order.Notes = dto.Notes;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            var updated = await _orderRepository.GetByIdAsync(order.Id);
            return OrderMapper.ToResponse(updated!);
        }

        public async Task<OrderResponseDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int employeeId)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            await EnsureCanUpdateStatusAsync(employeeId);

            _validator.ValidateStatusChange(order, dto);

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            await _statusHistoryRepository.AddAsync(new OrderStatusHistory
            {
                OrderId = order.Id,
                ChangedByEmployeeId = employeeId,
                Status = dto.Status,
                Notes = dto.Notes,
                ChangedAt = DateTime.UtcNow
            });
            await _statusHistoryRepository.SaveChangesAsync();

            // NUEVO: pago contra entrega confirmado manualmente por el cajero
            if (dto.Status == OrderStatus.Confirmed && order.OrderType == OrderType.Delivery)
                await _deliveryAssignmentService.AssignAutomaticallyAsync(order.Id);

            if (dto.Status == OrderStatus.Ready)
                await NotifyOrderReadyAsync(order);

            var updated = await _orderRepository.GetByIdAsync(order.Id);
            return OrderMapper.ToResponse(updated!);
        }

        // Avisa a Mesero y Cajero de la sede: el pedido está listo para
        // despachar/cobrar.
        private async Task NotifyOrderReadyAsync(Order order)
        {
            var userIds = await _employeeRepository.GetUserIdsByBranchAndRolesAsync(
                order.BranchId, RoleNames.Mesero, RoleNames.Cajero);

            foreach (var userId in userIds)
            {
                await _notificationService.CreateAsync(
                    userId,
                    "Pedido listo",
                    $"El pedido #{order.Id} está listo para despachar.",
                    SaborExpress.Modules.Notifications.Enum.NotificationType.OrderReady,
                    relatedEntityType: "Order",
                    relatedEntityId: order.Id);
            }
        }

               public async Task<OrderResponseDto> CancelAsync(int id, CancelOrderDto dto, int employeeId)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            await EnsureCanCancelAsync(employeeId);

            var result = await CancelCoreAsync(order, dto, employeeId);
            await NotifyOrderCancelledAsync(order, byCustomer: false);
            return result;
        }

        // NUEVO: cancelación hecha por el propio cliente desde la app
        public async Task<OrderResponseDto> CancelByCustomerAsync(int id, CancelOrderDto dto, int currentUserId)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            var user = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (user.Customer == null || order.CustomerId != user.Customer.Id)
                throw new InvalidOperationException("Este pedido no te pertenece.");

            // El cliente solo cancela antes de que cocina empiece
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                throw new InvalidOperationException(
                    "Tu pedido ya está en preparación y no se puede cancelar.");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                dto.Reason = "Cancelado por el cliente";

            var result = await CancelCoreAsync(order, dto, null);
            await NotifyOrderCancelledAsync(order, byCustomer: true);
            return result;
        }

        // Lógica común de cancelación (empleado o cliente)
        private async Task<OrderResponseDto> CancelCoreAsync(Order order, CancelOrderDto dto, int? employeeId)
        {
            _validator.ValidateCancel(order, dto);

            var payments = await _paymentRepository.GetByOrderIdAsync(order.Id);
            var totalPaid = payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);
            if (totalPaid >= order.Total)
            {
                throw new InvalidOperationException(
                    "Este pedido ya está pagado en su totalidad y no se puede cancelar.");
            }

            var currentDetails = await _orderDetailRepository.GetByOrderIdAsync(order.Id);
            if (currentDetails.Any(d =>
                    d.Status == OrderDetailStatus.InPreparation ||
                    d.Status == OrderDetailStatus.Ready ||
                    d.Status == OrderDetailStatus.Delivered))
            {
                throw new InvalidOperationException(
                    "Este pedido ya tiene platos en preparación, listos o entregados. " +
                    "No se puede cancelar mientras cocina ya empezó a prepararlo.");
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            foreach (var detail in currentDetails)
            {
                if (detail.Status == OrderDetailStatus.Delivered)
                    continue;

                detail.Status = OrderDetailStatus.Voided;
                await _orderDetailRepository.UpdateAsync(detail);
            }
            await _orderDetailRepository.SaveChangesAsync();

            if (order.TableId.HasValue)
            {
                var table = await _tableRepository.GetByIdAsync(order.TableId.Value);
                if (table != null && table.Status == TableStatus.Occupied)
                {
                    table.Status = TableStatus.Available;
                    await _tableRepository.UpdateAsync(table);
                    await _tableRepository.SaveChangesAsync();
                }
            }

            await _statusHistoryRepository.AddAsync(new OrderStatusHistory
            {
                OrderId = order.Id,
                ChangedByEmployeeId = employeeId,
                Status = OrderStatus.Cancelled,
                Notes = dto.Reason,
                ChangedAt = DateTime.UtcNow
            });
            await _statusHistoryRepository.SaveChangesAsync();

            var cancelled = await _orderRepository.GetByIdAsync(order.Id);
            return OrderMapper.ToResponse(cancelled!);
        }

        // Avisa al repartidor asignado y, si canceló el cliente, al cajero/admin de la sede.
        private async Task NotifyOrderCancelledAsync(Order order, bool byCustomer)
        {
            try
            {
                var userIds = new List<int>();

                var deliveries = await _deliveryRepository.GetByOrderIdAsync(order.Id);
                foreach (var d in deliveries.Where(d => d.Status != DeliveryStatus.Delivered))
                {
                    var uid = await _authRepository.GetUserIdByEmployeeIdAsync(d.DeliveryPersonId);
                    if (uid != null) userIds.Add(uid.Value);
                }

                if (byCustomer)
                {
                    var staff = await _employeeRepository.GetUserIdsByBranchAndRolesAsync(
                        order.BranchId, RoleNames.Cajero, RoleNames.Administrador);
                    userIds.AddRange(staff);
                }

                foreach (var userId in userIds.Distinct())
                {
                    await _notificationService.CreateAsync(
                        userId,
                        "Pedido cancelado",
                        $"El pedido #{order.Id} fue cancelado.",
                        NotificationType.OrderStatusChanged,
                        relatedEntityType: "Order",
                        relatedEntityId: order.Id);
                }
            }
            catch
            {
                // La cancelación ya quedó guardada; un fallo al notificar no debe romperla.
            }
        }

        private async Task EnsureBranchIsOpenAsync(int branchId)
        {
            var openingConfig = await _configurationRepository.GetByKeyAsync(branchId, "OPENING_TIME");
            var closingConfig = await _configurationRepository.GetByKeyAsync(branchId, "CLOSING_TIME");

            if (openingConfig == null || closingConfig == null)
                return;

            if (!TimeOnly.TryParse(openingConfig.Value, out var opening) ||
                !TimeOnly.TryParse(closingConfig.Value, out var closing))
            {
                return;
            }

            var now = TimeOnly.FromDateTime(DateTime.Now);

            var isOpen = opening < closing
                ? now >= opening && now <= closing
                : now >= opening || now <= closing;

            if (!isOpen)
                throw new InvalidOperationException($"La sede esta cerrada. Horario: {opening:HH:mm} - {closing:HH:mm}.");
        }

        private async Task EnsureCanEditOrderAsync(Order order, int employeeId)
        {
            var canEditAny = await _authorizationService.CanPerformActionAsync(employeeId, PermissionNames.EditarCualquierPedido);
            if (canEditAny)
                return;

            var canEditOwn = await _authorizationService.CanPerformActionAsync(employeeId, PermissionNames.EditarPedidoPropio);
            if (canEditOwn && order.EmployeeId == employeeId)
                return;

            throw new InvalidOperationException("No tienes permiso para modificar este pedido.");
        }

        private async Task EnsureCanUpdateStatusAsync(int employeeId)
        {
            var canUpdateStatus = await _authorizationService.CanPerformActionAsync(employeeId, PermissionNames.ActualizarEstadoPedido);
            if (!canUpdateStatus)
                throw new InvalidOperationException("No tienes permiso para cambiar el estado de este pedido.");
        }

        private async Task EnsureCanCancelAsync(int employeeId)
        {
            var canCancel = await _authorizationService.CanPerformActionAsync(employeeId, PermissionNames.CancelarPedido);
            if (!canCancel)
                throw new InvalidOperationException("No tienes permiso para cancelar pedidos.");
        }

        // Nuevo: si el usuario es Administrador (o cualquier no-Gerente), fuerza
        // el filtro a SU sede, sin importar qué branchId haya pedido o si no
        // mandó ninguno. Si es Gerente, respeta el filtro tal como vino.
        private async Task<int?> ResolveAllowedBranchIdAsync(int? requestedBranchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente))
                return requestedBranchId;

            var ownBranchId = currentUser.Employee?.BranchId
                ?? throw new InvalidOperationException("El usuario actual no tiene una sede asignada.");

            return ownBranchId;
        }

        // Nuevo: para el endpoint branch/{id}, donde el Id viene explícito en la URL
        private async Task EnsureCanAccessBranchAsync(int branchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente))
                return;

            if (currentUser.Employee?.BranchId != branchId)
                throw new InvalidOperationException("Solo puedes ver los pedidos de tu propia sede.");
        }
    }
}