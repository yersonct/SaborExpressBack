// Controllers/OrderDetailHistoryController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Orders.Interfaces;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/order-details")]
    [Authorize]
    public class OrderDetailHistoryController : ControllerBase
    {
        private readonly IOrderDetailHistoryService _historyService;

        public OrderDetailHistoryController(IOrderDetailHistoryService historyService)
        {
            _historyService = historyService;
        }

        // GET /api/order-details/{orderDetailId}/history
        [HttpGet("{orderDetailId:int}/history")]
        public async Task<IActionResult> GetHistory(int orderDetailId)
        {
            var result = await _historyService.GetByOrderDetailIdAsync(orderDetailId);
            return Ok(result);
        }
    }
}