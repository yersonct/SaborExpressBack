// Controllers/OrderDetailsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Constants;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Shared.Security;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class OrderDetailsController : ControllerBase
    {
        private readonly IOrderDetailService _orderDetailService;
        private readonly IOrderDetailRepository _orderDetailRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IAuthRepository _authRepository;

        public OrderDetailsController(
            IOrderDetailService orderDetailService,
            IOrderDetailRepository orderDetailRepository,
            IOrderRepository orderRepository,
            IAuthRepository authRepository)
        {
            _orderDetailService = orderDetailService;
            _orderDetailRepository = orderDetailRepository;
            _orderRepository = orderRepository;
            _authRepository = authRepository;
        }

        [HttpGet("orders/{orderId:int}/details")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var accessResult = await EnsureCanAccessOrderAsync(orderId);
            if (accessResult != null) return accessResult;

            var result = await _orderDetailService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        [HttpPost("orders/{orderId:int}/details")]
        [Authorize(Roles = $"{RoleNames.Mesero},{RoleNames.Cajero}")]
        public async Task<IActionResult> Create(int orderId, [FromBody] CreateOrderDetailDto dto)
        {
            var accessResult = await EnsureCanAccessOrderAsync(orderId);
            if (accessResult != null) return accessResult;

            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.CreateAsync(orderId, dto, employeeId);
            return CreatedAtAction(nameof(GetByOrder), new { orderId }, result);
        }
        [HttpPost("orders/{orderId:int}/details/batch")]
        public async Task<IActionResult> CreateBatch(int orderId, [FromBody] CreateOrderDetailsBatchDto dto)
        {
            var hasOperationalRole = User.IsInRole(RoleNames.Mesero)
                || User.IsInRole(RoleNames.Cajero)
                || User.IsInRole(RoleNames.Cocinero)
                || User.IsInRole(RoleNames.AuxiliarCocina)
                || User.IsInRole(RoleNames.Repartidor)
                || User.IsInRole(RoleNames.Administrador)
                || User.IsInRole(RoleNames.Gerente);
            var isCliente = User.IsInRole(RoleNames.Cliente) && !hasOperationalRole;
            if (!isCliente && !User.IsInRole(RoleNames.Mesero) && !User.IsInRole(RoleNames.Cajero))
                return Forbid();

            var accessResult = await EnsureCanAccessOrderAsync(orderId);
            if (accessResult != null) return accessResult;

            int? employeeId = isCliente ? null : this.GetCurrentEmployeeId();
            int? customerId = isCliente ? this.GetCurrentCustomerId() : null;

            var result = await _orderDetailService.CreateBatchAsync(orderId, dto, employeeId, customerId);
            return CreatedAtAction(nameof(GetByOrder), new { orderId }, result);
        }

        [HttpPatch("orders/{orderId:int}/details/batch/{batchNumber:int}/status")]
        [Authorize(Roles = $"{RoleNames.Cocinero},{RoleNames.AuxiliarCocina}")]
        public async Task<IActionResult> UpdateBatchStatus(int orderId, int batchNumber, [FromBody] UpdateBatchStatusDto dto)
        {
            var accessResult = await EnsureCanAccessOrderAsync(orderId);
            if (accessResult != null) return accessResult;

            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.UpdateBatchStatusAsync(orderId, batchNumber, dto, employeeId);
            return Ok(result);
        }

        [HttpPut("order-details/{id:int}")]
        [Authorize(Roles = $"{RoleNames.Mesero},{RoleNames.Cajero}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDetailDto dto)
        {
            var accessResult = await EnsureCanAccessDetailAsync(id);
            if (accessResult != null) return accessResult;

            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.UpdateAsync(id, dto, employeeId);
            return Ok(result);
        }

        [HttpPatch("order-details/{id:int}/to-go")]
        [Authorize(Roles = $"{RoleNames.Mesero},{RoleNames.Cajero}")]
        public async Task<IActionResult> UpdateToGo(
            int id,
            [FromBody] UpdateOrderDetailToGoDto dto)
        {
            var accessResult = await EnsureCanAccessDetailAsync(id);
            if (accessResult != null) return accessResult;

            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.UpdateToGoAsync(id, dto, employeeId);
            return Ok(result);
        }

        [HttpPatch("order-details/{id:int}/void")]
        [Authorize(Roles = $"{RoleNames.Mesero},{RoleNames.Cajero}")]
        public async Task<IActionResult> Void(int id, [FromBody] VoidOrderDetailDto dto)
        {
            var accessResult = await EnsureCanAccessDetailAsync(id);
            if (accessResult != null) return accessResult;

            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.VoidAsync(id, dto, employeeId);
            return Ok(result);
        }

        [HttpPatch("order-details/{id:int}/deliver")]
        [Authorize(Roles = RoleNames.Mesero)]
        public async Task<IActionResult> Deliver(int id)
        {
            var accessResult = await EnsureCanAccessDetailAsync(id);
            if (accessResult != null) return accessResult;

            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.MarkDeliveredAsync(id, employeeId);
            return Ok(result);
        }

        private async Task<IActionResult?> EnsureCanAccessOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return NotFound();

            var actor = await _authRepository.GetByIdWithRelationsAsync(this.GetCurrentUserId());
            if (actor == null || !ResourceReadPolicy.CanReadOrder(User, actor, order))
                return Forbid();

            return null;
        }

        private async Task<IActionResult?> EnsureCanAccessDetailAsync(int detailId)
        {
            var detail = await _orderDetailRepository.GetByIdAsync(detailId);
            if (detail == null) return NotFound();
            return await EnsureCanAccessOrderAsync(detail.OrderId);
        }
    }
}
