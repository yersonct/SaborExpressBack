// Controllers/InvoicesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Interfaces;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/Invoices")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        // POST /api/Invoices
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
        {
            var result = await _invoiceService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // GET /api/Invoices/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _invoiceService.GetByIdAsync(id);
            return Ok(result);
        }

        // GET /api/Invoices/payment/{paymentId}
        [HttpGet("payment/{paymentId:int}")]
        public async Task<IActionResult> GetByPayment(int paymentId)
        {
            var result = await _invoiceService.GetByPaymentIdAsync(paymentId);
            return Ok(result);
        }

        // GET /api/Invoices/order/{orderId}
        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var result = await _invoiceService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        // GET /api/Invoices/branch/{branchId}
        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var result = await _invoiceService.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        // GET /api/Invoices/{id}/pdf
        // TODO: requiere una librería de generación de PDF (ej. QuestPDF, iText7).
        // Este endpoint queda como placeholder hasta que se instale una de esas librerías.
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> GetPdf(int id)
        {
            await _invoiceService.GetByIdAsync(id); // valida que exista
            return StatusCode(501, new { message = "Generación de PDF pendiente de implementar. Ver TODO en InvoicesController." });
        }
    }
}
