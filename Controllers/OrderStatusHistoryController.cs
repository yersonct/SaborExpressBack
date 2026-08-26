// Controllers/OrderStatusHistoryController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Orders.Interfaces;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderStatusHistoryController : ControllerBase
    {
        private readonly IOrderStatusHistoryService _historyService;

        public OrderStatusHistoryController(IOrderStatusHistoryService historyService)
        {
            _historyService = historyService;
        }

        // GET /api/orders/{orderId}/status-history
        [HttpGet("{orderId:int}/status-history")]
        public async Task<IActionResult> GetHistory(int orderId)
        {
            var result = await _historyService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }
    }
}