// Controllers/OrderDetailHistoryController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Security;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/order-details")]
    [Authorize]
    public class OrderDetailHistoryController : ControllerBase
    {
        private readonly IOrderDetailHistoryService _historyService;
        private readonly IOrderDetailRepository _orderDetailRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IAuthRepository _authRepository;

        public OrderDetailHistoryController(
            IOrderDetailHistoryService historyService,
            IOrderDetailRepository orderDetailRepository,
            IOrderRepository orderRepository,
            IAuthRepository authRepository)
        {
            _historyService = historyService;
            _orderDetailRepository = orderDetailRepository;
            _orderRepository = orderRepository;
            _authRepository = authRepository;
        }

        // GET /api/order-details/{orderDetailId}/history
        [HttpGet("{orderDetailId:int}/history")]
        public async Task<IActionResult> GetHistory(int orderDetailId)
        {
            var detail = await _orderDetailRepository.GetByIdAsync(orderDetailId);
            if (detail == null) return NotFound();

            var order = await _orderRepository.GetByIdAsync(detail.OrderId);
            if (order == null) return NotFound();

            var actor = await _authRepository.GetByIdWithRelationsAsync(this.GetCurrentUserId());
            if (actor == null || !ResourceReadPolicy.CanReadOrder(User, actor, order))
                return Forbid();

            var result = await _historyService.GetByOrderDetailIdAsync(orderDetailId);
            return Ok(result);
        }
    }
}
