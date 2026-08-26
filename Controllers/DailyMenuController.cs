using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.DailyMenu.DTOs;
using SaborExpress.Modules.DailyMenu.Interfaces;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/daily-menu")]
    public class DailyMenuController : ControllerBase
    {
        private readonly IDailyMenuService _service;

        public DailyMenuController(IDailyMenuService service) => _service = service;

        // Cliente y Mesero: lo que ven para tomar el pedido hoy.
        [HttpGet("branch/{branchId:int}/today")]
        public async Task<IActionResult> GetTodayAvailable(int branchId, [FromQuery] string? period) =>
            Ok(await _service.GetTodayAvailableAsync(branchId, period));

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpGet("branch/{branchId:int}/date/{date:datetime}")]
        public async Task<IActionResult> GetByBranchAndDate(int branchId, DateTime date, [FromQuery] string? period) =>
            Ok(await _service.GetByBranchAndDateAsync(branchId, date, period));

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id) =>
            Ok(await _service.GetByIdAsync(id));

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDailyMenuItemDto dto)
        {
            var currentUserId = GetCurrentUserId();
            return StatusCode(201, await _service.CreateAsync(dto, currentUserId));
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkSet([FromBody] BulkSetDailyMenuDto dto)
        {
            var currentUserId = GetCurrentUserId();
            return Ok(await _service.BulkSetAsync(dto, currentUserId));
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDailyMenuItemDto dto)
        {
            var currentUserId = GetCurrentUserId();
            return Ok(await _service.UpdateAsync(id, dto, currentUserId));
        }

        // 👇 Único endpoint que también deja entrar al Cocinero
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Cocinero}")]
        [HttpPatch("{id:int}/toggle")]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var currentUserId = GetCurrentUserId();
            return Ok(await _service.ToggleAvailabilityAsync(id, currentUserId));
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = GetCurrentUserId();
            await _service.DeleteAsync(id, currentUserId);
            return NoContent();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Token inválido: no contiene el identificador del usuario.");

            return int.Parse(claim.Value);
        }
    }
}