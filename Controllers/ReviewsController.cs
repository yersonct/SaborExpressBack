// Controllers/ReviewsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Reviews.DTOs;
using SaborExpress.Modules.Reviews.Interfaces;
using SaborExpress.Shared.Extensions;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/Reviews")]
    [Authorize]
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
            var customerId = this.GetCurrentCustomerId();
            var result = await _reviewService.CreateAsync(dto, customerId);
            return CreatedAtAction(nameof(GetByOrder), new { orderId = result.OrderId }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewDto dto)
        {
            var customerId = this.GetCurrentCustomerId();
            var result = await _reviewService.UpdateAsync(id, dto, customerId);
            return Ok(result);
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
            if (User.IsInRole("CLIENTE") && this.GetCurrentCustomerId() != customerId)
                return Forbid();

            var result = await _reviewService.GetByCustomerIdAsync(customerId);
            return Ok(result);
        }

        // CAMBIADO: ahora pasa currentUserId para que el service valide la sede
        [HttpGet("branch/{branchId:int}")]
        [Authorize(Roles = "GERENTE,ADMINISTRADOR")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var currentUserId = this.GetCurrentUserId();
            var result = await _reviewService.GetByBranchIdAsync(branchId, currentUserId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId:int}/summary")]
        [Authorize(Roles = "GERENTE")]
        public async Task<IActionResult> GetBranchSummary(int branchId)
        {
            var result = await _reviewService.GetBranchSummaryAsync(branchId);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = this.GetCurrentUserId();
            await _reviewService.DeleteAsync(id, currentUserId);
            return NoContent();
        }
    }
}