using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Orders.Interfaces;

namespace SaborExpress.Controllers
{
    // Sin [Authorize] a propósito: esta ruta es pública, pensada para un monitor
    // de cocina fijo sin login. La "seguridad" es el código de sede en la URL,
    // que no es el Id real y solo tú lo conoces/compartes con cada sede.
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