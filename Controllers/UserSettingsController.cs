using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.UserPreferences.DTOs;
using SaborExpress.Modules.UserPreferences.Interfaces;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/user-settings")]
    [Authorize]
    public class UserSettingsController : ControllerBase
    {
        private readonly IUserSettingsService _service;

        public UserSettingsController(IUserSettingsService service) => _service = service;

        // GET /api/user-settings/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
            => Ok(await _service.GetAsync(GetCurrentUserId()));

        // PUT /api/user-settings/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMine([FromBody] UpdateUserSettingsDto dto)
            => Ok(await _service.UpdateAsync(dto, GetCurrentUserId()));

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Token inválido: no contiene el identificador del usuario.");
            return id;
        }
    }
}