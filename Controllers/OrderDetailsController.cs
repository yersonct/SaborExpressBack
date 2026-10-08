// Controllers/OrderDetailsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class OrderDetailsController : ControllerBase
    {
        private readonly IOrderDetailService _orderDetailService;

        public OrderDetailsController(IOrderDetailService orderDetailService)
        {
            _orderDetailService = orderDetailService;
        }

        [HttpGet("orders/{orderId:int}/details")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var result = await _orderDetailService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        [HttpPost("orders/{orderId:int}/details")]
        public async Task<IActionResult> Create(int orderId, [FromBody] CreateOrderDetailDto dto)
        {
            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.CreateAsync(orderId, dto, employeeId);
            return CreatedAtAction(nameof(GetByOrder), new { orderId }, result);
        }
        [HttpPost("orders/{orderId:int}/details/batch")]
        public async Task<IActionResult> CreateBatch(int orderId, [FromBody] CreateOrderDetailsBatchDto dto)
        {
            var isCliente = User.IsInRole(RoleNames.Cliente);
            int? employeeId = isCliente ? null : this.GetCurrentEmployeeId();
            int? customerId = isCliente ? this.GetCurrentCustomerId() : null;

            var result = await _orderDetailService.CreateBatchAsync(orderId, dto, employeeId, customerId);
            return CreatedAtAction(nameof(GetByOrder), new { orderId }, result);
        }

        [HttpPatch("orders/{orderId:int}/details/batch/{batchNumber:int}/status")]
        public async Task<IActionResult> UpdateBatchStatus(int orderId, int batchNumber, [FromBody] UpdateBatchStatusDto dto)
        {
            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.UpdateBatchStatusAsync(orderId, batchNumber, dto, employeeId);
            return Ok(result);
        }

        [HttpPut("order-details/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrderDetailDto dto)
        {
            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.UpdateAsync(id, dto, employeeId);
            return Ok(result);
        }
        [HttpPatch("order-details/{id:int}/void")]
        public async Task<IActionResult> Void(int id, [FromBody] VoidOrderDetailDto dto)
        {
            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.VoidAsync(id, dto, employeeId);
            return Ok(result);
        }

        [HttpPatch("order-details/{id:int}/deliver")]
        public async Task<IActionResult> Deliver(int id)
        {
            var employeeId = this.GetCurrentEmployeeId();
            var result = await _orderDetailService.MarkDeliveredAsync(id, employeeId);
            return Ok(result);
        }
    }
}