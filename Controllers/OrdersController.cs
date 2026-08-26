// Controllers/OrdersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    
    [Authorize]
    [ApiController]
    [Route("api/Orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            var employeeId = this.GetCurrentEmployeeId();
            var isClienteChannel = User.IsInRole(RoleNames.Cliente); 
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
            var result = await _orderService.GetAllAsync(filter);
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
            var result = await _orderService.GetByBranchIdAsync(branchId);
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
            var result = await _orderService.UpdateStatusAsync(id, dto, employeeId);
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