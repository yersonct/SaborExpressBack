// Controllers/UserPreferencesController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.UserPreferences.DTOs;
using SaborExpress.Modules.UserPreferences.Interfaces;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/user-preferences")]
    [Authorize] // nadie sin token entra a este módulo
    public class UserPreferencesController : ControllerBase
    {
        private readonly IUserPreferenceService _preferenceService;

        public UserPreferencesController(IUserPreferenceService preferenceService)
        {
            _preferenceService = preferenceService;
        }

        // GET /api/user-preferences/user/{userId}
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var currentUserId = GetCurrentUserId();

            // Solo el dueño puede ver sus propias preferencias.
            // Si más adelante quieres que Gerente/Administrador vean las de cualquiera,
            // aquí es donde se agrega el chequeo de rol (mismo patrón que Addresses/Reviews).
            if (userId != currentUserId)
                return Forbid();

            var result = await _preferenceService.GetByUserIdAsync(userId);
            return Ok(result);
        }

        // POST /api/user-preferences
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserPreferenceDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _preferenceService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetByUser), new { userId }, result);
        }

        // PUT /api/user-preferences/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserPreferenceDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _preferenceService.UpdateAsync(id, dto, userId);
            return Ok(result);
        }

        // DELETE /api/user-preferences/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            await _preferenceService.DeleteAsync(id, userId);
            return NoContent();
        }

        // GET /api/user-preferences/language
        // Devuelve el idioma actual del usuario logueado (o "es" por defecto si nunca lo configuró)
        [HttpGet("language")]
        public async Task<IActionResult> GetLanguage()
        {
            var userId = GetCurrentUserId();
            var result = await _preferenceService.GetLanguageAsync(userId);
            return Ok(result);
        }

        // PUT /api/user-preferences/language
        // Cambia el idioma del usuario logueado. Sirve para cliente o empleado, ambos usan User.
        [HttpPut("language")]
        public async Task<IActionResult> SetLanguage([FromBody] SetLanguageDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _preferenceService.SetLanguageAsync(dto, userId);
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Token inválido: no contiene el identificador del usuario.");
            return id;
        }
    }
}