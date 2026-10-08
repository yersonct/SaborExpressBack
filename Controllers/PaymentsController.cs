// Controllers/PaymentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Shared.Extensions;
using System.Security.Claims;
using System.Text.Json;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/Payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        // POST /api/Payments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            var cashierId = this.GetCurrentEmployeeId();
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
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        public async Task<IActionResult> GetAll([FromQuery] PaymentFilterDto filter)
        {
            var result = await _paymentService.GetAllAsync(filter);
            return Ok(result);
        }

        // GET /api/Payments/my-shift — cierre de caja del Cajero autenticado (solo sus pagos de hoy)
        [HttpGet("my-shift")]
        public async Task<IActionResult> GetMyShift()
        {
            var cashierId = this.GetCurrentEmployeeId();
            var result = await _paymentService.GetMyShiftSummaryAsync(cashierId);
            return Ok(result);
        }

        // POST /api/Payments/wompi/init
        // El cliente inicia el pago desde el checkout de la app.
        // Devuelve los datos que el widget de Wompi necesita para renderizarse
        // (llave pública, referencia, monto en centavos, firma de integridad).
        [HttpPost("wompi/init")]
        public async Task<IActionResult> InitWompiPayment([FromBody] InitWompiPaymentDto dto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _paymentService.InitWompiPaymentAsync(dto, currentUserId);
            return Ok(result);
        }

                // POST /api/Payments/wompi/cashier-init
        // El cajero genera el cobro por QR. Devuelve los datos para armar el link de Wompi.
        [HttpPost("wompi/cashier-init")]
        public async Task<IActionResult> InitCashierWompi([FromBody] InitCashierWompiPaymentDto dto)
        {
            var cashierId = this.GetCurrentEmployeeId();
            var result = await _paymentService.InitCashierWompiPaymentAsync(dto, cashierId, GetCurrentUserId());
            return Ok(result);
        }

        // POST /api/Payments/{id}/wompi-sync
        // Respaldo: si el webhook no llegó, el back le pregunta a Wompi el estado real.
        [HttpPost("{id:int}/wompi-sync")]
        public async Task<IActionResult> SyncWompi(int id)
        {
            var result = await _paymentService.SyncWompiPaymentAsync(id, GetCurrentUserId());
            return Ok(result);
        }

        // POST /api/Payments/wompi/webhook
        [AllowAnonymous]
        [HttpPost("wompi/webhook")]
        public async Task<IActionResult> WompiWebhook()
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            _logger.LogInformation("Webhook Wompi recibido: {Body}", rawBody);

            var signatureHeader = Request.Headers["X-Event-Checksum"].FirstOrDefault() ?? string.Empty;

            WompiWebhookDto? webhook;
            try
            {
                webhook = JsonSerializer.Deserialize<WompiWebhookDto>(
                    rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Webhook Wompi con JSON inválido");
                return BadRequest();
            }

            if (webhook == null)
                return BadRequest();

            await _paymentService.ProcessWompiWebhookAsync(webhook, rawBody, signatureHeader);

            // Wompi solo necesita un 200 para no reintentar el envío
            return Ok();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Token inválido: no contiene el identificador del usuario.");

            return int.Parse(claim.Value);
        }
    }
}