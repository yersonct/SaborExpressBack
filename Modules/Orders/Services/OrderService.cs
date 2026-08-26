// Modules/Orders/Services/OrderService.cs
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Mappings;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Orders.Validators;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Shared.Constants;


namespace SaborExpress.Modules.Orders.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderStatusHistoryRepository _statusHistoryRepository;
        private readonly OrderValidator _validator;
        private readonly IAuthorizationService _authorizationService;
        private readonly IConfigurationRepository _configurationRepository;

        public OrderService(
            IOrderRepository orderRepository,
            IOrderStatusHistoryRepository statusHistoryRepository,
            OrderValidator validator,
            IAuthorizationService authorizationService,
            IConfigurationRepository configurationRepository)
        {
            _orderRepository = orderRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _validator = validator;
            _authorizationService = authorizationService;
            _configurationRepository = configurationRepository;
        }

        public async Task<OrderResponseDto> CreateAsync(CreateOrderDto dto, int employeeId, bool isClienteChannel)
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
                Notes = "Pedido creado",
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

        public async Task<List<OrderSummaryDto>> GetAllAsync(OrderFilterDto filter)
        {
            var orders = await _orderRepository.GetAllAsync(filter.BranchId, filter.Status);
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

        public async Task<List<OrderSummaryDto>> GetByBranchIdAsync(int branchId)
        {
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

        // Nuevo: valida que la sede esté dentro de su horario de apertura/cierre
        // configurado en BranchSetting (Key = OPENING_TIME / CLOSING_TIME).
        // Si la sede no tiene esas configuraciones, se asume abierta 24h.
        private async Task EnsureBranchIsOpenAsync(int branchId)
        {
            var openingConfig = await _configurationRepository.GetByKeyAsync(branchId, "OPENING_TIME");
            var closingConfig = await _configurationRepository.GetByKeyAsync(branchId, "CLOSING_TIME");

            if (openingConfig == null || closingConfig == null)
                return;

            if (!TimeOnly.TryParse(openingConfig.Value, out var opening) ||
                !TimeOnly.TryParse(closingConfig.Value, out var closing))
            {
                return; // configuracion mal cargada, no bloquea pedidos por un dato invalido
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
    }
}