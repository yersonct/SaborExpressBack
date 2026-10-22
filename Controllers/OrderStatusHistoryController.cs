// Controllers/OrderStatusHistoryController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Security;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderStatusHistoryController : ControllerBase
    {
        private readonly IOrderStatusHistoryService _historyService;
        private readonly IOrderRepository _orderRepository;
        private readonly IAuthRepository _authRepository;

        public OrderStatusHistoryController(
            IOrderStatusHistoryService historyService,
            IOrderRepository orderRepository,
            IAuthRepository authRepository)
        {
            _historyService = historyService;
            _orderRepository = orderRepository;
            _authRepository = authRepository;
        }

        // GET /api/orders/{orderId}/status-history
        [HttpGet("{orderId:int}/status-history")]
        public async Task<IActionResult> GetHistory(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return NotFound();

            var actor = await _authRepository.GetByIdWithRelationsAsync(this.GetCurrentUserId());
            if (actor == null || !ResourceReadPolicy.CanReadOrder(User, actor, order))
                return Forbid();

            var result = await _historyService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }
    }
}
