using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Customers.DTOs;
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Shared.Constants;
using System.Security.Claims;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
            => _customerService = customerService;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCustomerDto dto) =>
            Ok(await _customerService.RegisterAsync(dto));

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = GetCurrentUserId();
            return Ok(await _customerService.GetMeAsync(userId));
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateCustomerDto dto)
        {
            var userId = GetCurrentUserId();
            return Ok(await _customerService.UpdateMeAsync(userId, dto));
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _customerService.GetAllAsync());

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _customerService.GetByIdAsync(id));

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Token inválido: no contiene el identificador del usuario.");

            return int.Parse(claim.Value);
        }
    }
}