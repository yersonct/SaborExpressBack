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

        public OrdersController(IOrderService orderService, IPaymentRepository paymentRepository)
        {
            _orderService = orderService;
            _paymentRepository = paymentRepository;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            var isClienteChannel = User.IsInRole(RoleNames.Cliente);
            int? employeeId = isClienteChannel ? null : this.GetCurrentEmployeeId();

            var result = await _orderService.CreateAsync(dto, employeeId, isClienteChannel);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _orderService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] OrderFilterDto filter)
        {
            var currentUserId = this.GetCurrentUserId();
            var result = await _orderService.GetAllAsync(filter, currentUserId);
            return Ok(result);
        }

        [HttpGet("table/{tableId:int}")]
        public async Task<IActionResult> GetByTable(int tableId)
        {
            var result = await _orderService.GetByTableIdAsync(tableId);
            return Ok(result);
        }

        [HttpGet("customer/{customerId:int}")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var result = await _orderService.GetByCustomerIdAsync(customerId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var currentUserId = this.GetCurrentUserId();
            var result = await _orderService.GetByBranchIdAsync(branchId, currentUserId);
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

        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelOrderDto dto)
        {
            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderService.CancelAsync(id, dto, employeeId);
            return Ok(result);
        }
    }
}