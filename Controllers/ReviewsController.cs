// Controllers/ReviewsController.cs
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Reviews.DTOs;
using SaborExpress.Modules.Reviews.Interfaces;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/Reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            var customerId = GetCurrentCustomerId(); // TODO: conectar con Auth (cliente autenticado)
            var result = await _reviewService.CreateAsync(dto, customerId);
            return CreatedAtAction(nameof(GetByOrder), new { orderId = result.OrderId }, result);
        }

        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var result = await _reviewService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        [HttpGet("customer/{customerId:int}")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var result = await _reviewService.GetByCustomerIdAsync(customerId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var result = await _reviewService.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        // TODO: restringir a rol Gerente/Admin (moderación)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _reviewService.DeleteAsync(id);
            return NoContent();
        }

        private int GetCurrentCustomerId()
        {
            var claim = User.FindFirst("CustomerId")?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}