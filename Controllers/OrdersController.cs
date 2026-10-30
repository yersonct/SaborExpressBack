// Controllers/OrdersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Constants;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Shared.Security;

namespace SaborExpress.Controllers
{
    [Authorize]
    [Authorize(Policy = "ActiveShift")] 
    [ApiController]
    [Route("api/Orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ITableRepository _tableRepository;

        public OrdersController(
            IOrderService orderService,
            IPaymentRepository paymentRepository,
            IAuthRepository authRepository,
            IOrderRepository orderRepository,
            ITableRepository tableRepository)
        {
            _orderService = orderService;
            _paymentRepository = paymentRepository;
            _authRepository = authRepository;
            _orderRepository = orderRepository;
            _tableRepository = tableRepository;
        }

        private async Task<SaborExpress.Modules.Auth.Models.User> GetActorAsync()
            => await _authRepository.GetByIdWithRelationsAsync(this.GetCurrentUserId())
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            var hasOperationalShiftRole =
                User.IsInRole(RoleNames.Mesero) ||
                User.IsInRole(RoleNames.Cajero) ||
                User.IsInRole(RoleNames.Cocinero) ||
                User.IsInRole(RoleNames.AuxiliarCocina) ||
                User.IsInRole(RoleNames.Repartidor);
            var isClienteChannel = User.IsInRole(RoleNames.Cliente) && !hasOperationalShiftRole;
            int? employeeId = isClienteChannel ? null : this.GetCurrentEmployeeId();

            var result = await _orderService.CreateAsync(dto, employeeId, isClienteChannel);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null) return NotFound();

            var actor = await GetActorAsync();
            if (!ResourceReadPolicy.CanReadOrder(User, actor, order)) return Forbid();

            var result = await _orderService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "GERENTE,ADMINISTRADOR,MESERO,CAJERO,COCINERO,AUXILIAR_COCINA")]
        public async Task<IActionResult> GetAll([FromQuery] OrderFilterDto filter)
        {
            var actor = await GetActorAsync();
            if (!User.IsInRole(RoleNames.Gerente))
            {
                var branchId = actor.Employee?.BranchId;
                if (!branchId.HasValue ||
                    (filter.BranchId.HasValue && filter.BranchId != branchId)) return Forbid();

                // No confiar en los roles persistentes del servicio: forzar la sede efectiva.
                filter.BranchId = branchId;
            }

            if (User.IsInRole(RoleNames.Mesero)) filter.EmployeeId = actor.Employee?.Id;
            var result = await _orderService.GetAllAsync(filter, actor.Id);
            // No ampliar la visibilidad del MESERO si tiene otros roles historicos asignados.
            if (User.IsInRole(RoleNames.Mesero))
                return Ok(result.Where(order => order.EmployeeId == actor.Employee?.Id).ToList());
            return Ok(result);
        }

        [HttpGet("table/{tableId:int}")]
        [Authorize(Roles = "GERENTE,ADMINISTRADOR,MESERO,CAJERO,COCINERO,AUXILIAR_COCINA")]
        public async Task<IActionResult> GetByTable(int tableId)
        {
            var table = await _tableRepository.GetByIdAsync(tableId);
            if (table == null) return NotFound();

            var actor = await GetActorAsync();
            if (!ResourceReadPolicy.CanReadBranch(User, actor, table.BranchId)) return Forbid();

            var result = await _orderService.GetByTableIdAsync(tableId);
            return Ok(result);
        }

        [HttpGet("customer/{customerId:int}")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var actor = await GetActorAsync();
            if (User.IsInRole(RoleNames.Cliente) && actor.Customer?.Id != customerId)
                return Forbid();

            var result = await _orderService.GetByCustomerIdAsync(customerId);
            // Para empleados, limitar al ámbito permitido; repartidores, solo asignados.
            var visible = result.Where(order =>
                ResourceReadPolicy.CanReadOrderSummary(User, actor, order)).ToList();
            return Ok(visible);
        }

        [HttpGet("branch/{branchId:int}")]
        [Authorize(Roles = "GERENTE,ADMINISTRADOR,MESERO,CAJERO,COCINERO,AUXILIAR_COCINA")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var actor = await GetActorAsync();
            if (!ResourceReadPolicy.CanReadBranch(User, actor, branchId)) return Forbid();

            var result = await _orderService.GetByBranchIdAsync(branchId, actor.Id);
            return Ok(result);
        }

        [HttpGet("branch/{branchId:int}/occupied-tables")]
        [Authorize(Roles = "GERENTE,ADMINISTRADOR,MESERO,CAJERO,COCINERO,AUXILIAR_COCINA")]
        public async Task<IActionResult> GetOccupiedTableWaiters(int branchId)
        {
            var actor = await GetActorAsync();
            if (!ResourceReadPolicy.CanReadBranch(User, actor, branchId)) return Forbid();

            var result = await _orderService.GetOccupiedTableWaitersByBranchIdAsync(
                branchId, actor.Id);
            return Ok(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDto dto)
        {
            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderService.UpdateAsync(id, dto, employeeId);
            return Ok(result);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            // El panel de Gerente/Administrador es solo de supervisión.
            if (User.IsInRole(RoleNames.Gerente) || User.IsInRole(RoleNames.Administrador))
                return StatusCode(403, new { message = "Gerente y Administrador no pueden cambiar el estado de los pedidos." });

            var employeeId = this.GetCurrentEmployeeId();

            // Si el cliente está pagando con Wompi, no se confirma a mano:
            // el webhook lo confirma cuando llegue el pago aprobado.
            // Solo cuenta si el intento es reciente (30 min), para que un pago
            // abandonado no bloquee el pedido para siempre.
            if (dto.Status == OrderStatus.Confirmed)
            {
                var payments = await _paymentRepository.GetByOrderIdAsync(id);
                var waitingForWompi = payments.Any(p =>
                    p.Method == PaymentMethod.Wompi
                    && p.Status == PaymentStatus.Pending
                    && p.PaidAt > DateTime.UtcNow.AddMinutes(-30));

                if (waitingForWompi)
                    return BadRequest(new
                    {
                        message = "El cliente está pagando en línea con Wompi. El pedido se confirmará solo cuando el pago sea aprobado."
                    });
            }

            var result = await _orderService.UpdateStatusAsync(id, dto, employeeId);
            return Ok(result);
        }

        // El cliente cancela su propio pedido (solo antes de que cocina empiece)
        [HttpPatch("{id:int}/cancel-mine")]
        [Authorize(Roles = RoleNames.Cliente)]
        public async Task<IActionResult> CancelMine(int id, [FromBody] CancelOrderDto dto)
        {
            var userId = this.GetCurrentUserId();
            var result = await _orderService.CancelByCustomerAsync(id, dto, userId);
            return Ok(result);
        }

        // El cliente avisa que pagará en efectivo contra entrega
        [HttpPost("{id:int}/confirm-cash")]
        [Authorize(Roles = RoleNames.Cliente)]
        public async Task<IActionResult> ConfirmCash(int id)
        {
            var userId = this.GetCurrentUserId();
            await _orderService.ConfirmCashDeliveryAsync(id, userId);
            return NoContent();
        }

        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelOrderDto dto)
        {
            if (User.IsInRole(RoleNames.Gerente) || User.IsInRole(RoleNames.Administrador))
                return StatusCode(403, new { message = "Gerente y Administrador no pueden cancelar pedidos." });

            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderService.CancelAsync(id, dto, employeeId);
            return Ok(result);
        }
    }
}
