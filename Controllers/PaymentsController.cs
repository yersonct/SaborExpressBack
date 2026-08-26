// Controllers/PaymentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Shared.Extensions;
using System.Security.Claims;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/Payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // POST /api/Payments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            var cashierId = this.GetCurrentEmployeeId(); // TODO: en pagos en línea, usar un "cajero de sistema"
            var currentUserId = GetCurrentUserId();
            var result = await _paymentService.CreateAsync(dto, cashierId, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // GET /api/Payments/order/{orderId}
        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var result = await _paymentService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        // GET /api/Payments/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _paymentService.GetByIdAsync(id);
            return Ok(result);
        }

        // PATCH /api/Payments/{id}/refund
        [HttpPatch("{id:int}/refund")]
        public async Task<IActionResult> Refund(int id, [FromBody] RefundPaymentDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _paymentService.RefundAsync(id, dto, currentUserId);
            return Ok(result);
        }

        // GET /api/Payments  (filtrable por sucursal/fecha - Gerente/Admin)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaymentFilterDto filter)
        {
            var result = await _paymentService.GetAllAsync(filter);
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Token inválido: no contiene el identificador del usuario.");

            return int.Parse(claim.Value);
        }
    }
}