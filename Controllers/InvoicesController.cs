// Controllers/InvoicesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Interfaces;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Security;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/Invoices")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IAuthRepository _authRepository;

        public InvoicesController(
            IInvoiceService invoiceService,
            IInvoiceRepository invoiceRepository,
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IAuthRepository authRepository)
        {
            _invoiceService = invoiceService;
            _invoiceRepository = invoiceRepository;
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _authRepository = authRepository;
        }

        // POST /api/Invoices
        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Cajero}")]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
        {
            var payment = await _paymentRepository.GetByIdAsync(dto.PaymentId);
            if (payment == null) return NotFound();

            var accessResult = await EnsureCanReadOrderAsync(payment.OrderId);
            if (accessResult != null) return accessResult;

            var result = await _invoiceService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // GET /api/Invoices/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            var accessResult = await EnsureCanReadOrderAsync(invoice.OrderId);
            if (accessResult != null) return accessResult;

            var result = await _invoiceService.GetByIdAsync(id);
            return Ok(result);
        }

        // GET /api/Invoices/payment/{paymentId}
        [HttpGet("payment/{paymentId:int}")]
        public async Task<IActionResult> GetByPayment(int paymentId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null) return NotFound();

            var accessResult = await EnsureCanReadOrderAsync(payment.OrderId);
            if (accessResult != null) return accessResult;

            var result = await _invoiceService.GetByPaymentIdAsync(paymentId);
            return Ok(result);
        }

        // GET /api/Invoices/order/{orderId}
        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var accessResult = await EnsureCanReadOrderAsync(orderId);
            if (accessResult != null) return accessResult;

            var result = await _invoiceService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        // GET /api/Invoices/branch/{branchId}
        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var actor = await _authRepository.GetByIdWithRelationsAsync(this.GetCurrentUserId());
            if (actor == null || !ResourceReadPolicy.CanReadBranch(User, actor, branchId))
                return Forbid();

            var result = await _invoiceService.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        // GET /api/Invoices/{id}/pdf
        // TODO: requiere una libreria de generacion de PDF (ej. QuestPDF, iText7).
        // Este endpoint queda como placeholder hasta que se instale una de esas librerias.
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> GetPdf(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            var accessResult = await EnsureCanReadOrderAsync(invoice.OrderId);
            if (accessResult != null) return accessResult;

            await _invoiceService.GetByIdAsync(id);
            return StatusCode(501, new { message = "Generacion de PDF pendiente de implementar. Consulta el TODO en InvoicesController." });
        }

        private async Task<IActionResult?> EnsureCanReadOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return NotFound();

            var actor = await _authRepository.GetByIdWithRelationsAsync(this.GetCurrentUserId());
            if (actor == null || !ResourceReadPolicy.CanReadPaymentOfOrder(User, actor, order))
                return Forbid();

            return null;
        }
    }
}
