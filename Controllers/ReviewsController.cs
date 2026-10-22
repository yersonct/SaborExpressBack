using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Reviews.DTOs;
using SaborExpress.Modules.Reviews.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Security;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/Reviews")]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly IReviewRepository _reviewRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IAuthRepository _authRepository;

        public ReviewsController(
            IReviewService reviewService,
            IReviewRepository reviewRepository,
            IOrderRepository orderRepository,
            IAuthRepository authRepository)
        {
            _reviewService = reviewService;
            _reviewRepository = reviewRepository;
            _orderRepository = orderRepository;
            _authRepository = authRepository;
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Cliente)]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            var customerId = this.GetCurrentCustomerId();
            var result = await _reviewService.CreateAsync(dto, customerId);
            return CreatedAtAction(nameof(GetByOrder), new { orderId = result.OrderId }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleNames.Cliente)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewDto dto)
        {
            var customerId = this.GetCurrentCustomerId();
            var result = await _reviewService.UpdateAsync(id, dto, customerId);
            return Ok(result);
        }

        [HttpGet("order/{orderId:int}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
            var accessResult = await EnsureCanReadOrderAsync(orderId);
            if (accessResult != null) return accessResult;

            var result = await _reviewService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        [HttpGet("customer/{customerId:int}")]
        [Authorize(Roles = RoleNames.Cliente)]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            if (this.GetCurrentCustomerId() != customerId)
                return Forbid();

            var result = await _reviewService.GetByCustomerIdAsync(customerId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId:int}")]
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var scopeResult = await EnsureCanReadBranchAsync(branchId);
            if (scopeResult != null) return scopeResult;

            var currentUserId = this.GetCurrentUserId();
            var result = await _reviewService.GetByBranchIdAsync(branchId, currentUserId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId:int}/summary")]
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        public async Task<IActionResult> GetBranchSummary(int branchId)
        {
            var scopeResult = await EnsureCanReadBranchAsync(branchId);
            if (scopeResult != null) return scopeResult;

            var result = await _reviewService.GetBranchSummaryAsync(branchId);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{RoleNames.Cliente},{RoleNames.Administrador},{RoleNames.Gerente}")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _reviewRepository.GetByIdWithOrderAsync(id);
            if (review == null) return NotFound();

            var currentUserId = this.GetCurrentUserId();
            var actor = await _authRepository.GetByIdWithRelationsAsync(currentUserId);
            if (actor == null) return Forbid();

            var isManager = User.IsInRole(RoleNames.Gerente);
            var isAdministrator = User.IsInRole(RoleNames.Administrador);
            var isOwner = User.IsInRole(RoleNames.Cliente)
                && actor.Customer?.Id == review.CustomerId;

            if (!isManager
                && !(isAdministrator && actor.Employee?.BranchId == review.Order.BranchId)
                && !isOwner)
                return Forbid();

            await _reviewService.DeleteAsync(id, currentUserId, isManager, isAdministrator);
            return NoContent();
        }

        private async Task<IActionResult?> EnsureCanReadOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return NotFound();

            var actor = await _authRepository.GetByIdWithRelationsAsync(this.GetCurrentUserId());
            if (actor == null || !ResourceReadPolicy.CanReadOrder(User, actor, order))
                return Forbid();

            return null;
        }

        private async Task<IActionResult?> EnsureCanReadBranchAsync(int branchId)
        {
            var actor = await _authRepository.GetByIdWithRelationsAsync(this.GetCurrentUserId());
            if (actor == null || !ResourceReadPolicy.CanReadBranch(User, actor, branchId))
                return Forbid();

            return null;
        }
    }
}
