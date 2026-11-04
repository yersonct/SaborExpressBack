using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Orders.Interfaces;

namespace SaborExpress.Controllers
{
    // Enlace revocable por turno; solo expone datos operativos de pedidos activos.
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ApiController]
    [Route("api/public/kitchen-board")]
    public class PublicKitchenController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public PublicKitchenController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("token/{accessToken}")]
        public async Task<IActionResult> GetBoard(string accessToken)
        {
            var result = await _orderService.GetKitchenBoardByAccessTokenAsync(accessToken);
            return Ok(result);
        }
    }
}