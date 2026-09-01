// Modules/Orders/Services/OrderService.cs
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

namespace SaborExpress.Modules.Orders.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderStatusHistoryRepository _statusHistoryRepository;
        private readonly OrderValidator _validator;
        private readonly IAuthorizationService _authorizationService;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IAuthRepository _authRepository;

        public OrderService(
            IOrderRepository orderRepository,
            IOrderStatusHistoryRepository statusHistoryRepository,
            OrderValidator validator,
            IAuthorizationService authorizationService,
            IConfigurationRepository configurationRepository,
            IAuthRepository authRepository)
        {
            _orderRepository = orderRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _validator = validator;
            _authorizationService = authorizationService;
            _configurationRepository = configurationRepository;
            _authRepository = authRepository;
        }

        public async Task<OrderResponseDto> CreateAsync(CreateOrderDto dto, int? employeeId, bool isClienteChannel)
        {
            await EnsureBranchIsOpenAsync(dto.BranchId);

            await _validator.ValidateCreateAsync(dto, employeeId);

            var order = new Order
            {
                CustomerId = dto.CustomerId,
                EmployeeId = employeeId,
                TableId = dto.TableId,
                BranchId = dto.BranchId,
                OrderType = dto.OrderType,
                Channel = isClienteChannel ? OrderChannel.App : OrderChannel.Mostrador,
                Notes = dto.Notes,
                Status = OrderStatus.Pending,
                SubTotal = 0,
                Tax = 0,
                Total = 0
            };

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            await _statusHistoryRepository.AddAsync(new OrderStatusHistory
            {
                OrderId = order.Id,
                ChangedByEmployeeId = employeeId,
                Status = OrderStatus.Pending,
                Notes = isClienteChannel ? "Pedido creado por el cliente desde la app" : "Pedido creado",
                ChangedAt = DateTime.UtcNow
            });
            await _statusHistoryRepository.SaveChangesAsync();

            var created = await _orderRepository.GetByIdAsync(order.Id);
            return OrderMapper.ToResponse(created!);
        }

        public async Task<OrderResponseDto> GetByIdAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            return OrderMapper.ToResponse(order);
        }

        // CAMBIADO: recibe currentUserId, fuerza la sede si el usuario no es Gerente
        public async Task<List<OrderSummaryDto>> GetAllAsync(OrderFilterDto filter, int currentUserId)
        {
            var effectiveBranchId = await ResolveAllowedBranchIdAsync(filter.BranchId, currentUserId);

            var orders = await _orderRepository.GetAllAsync(effectiveBranchId, filter.Status);
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

            var updated = await _orderRepository.GetByIdAsync(order.Id);
            return OrderMapper.ToResponse(updated!);
        }

        public async Task<OrderResponseDto> CancelAsync(int id, CancelOrderDto dto, int employeeId)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            await EnsureCanCancelAsync(employeeId);

            _validator.ValidateCancel(order, dto);

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

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