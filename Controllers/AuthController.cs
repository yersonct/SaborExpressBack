using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Interfaces;
using System;
using System.Threading.Tasks;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var response = await _authService.RegisterAsync(dto);

                // Retorna 201 Created con el token y los datos del usuario
                return StatusCode(201, response);
            }
            catch (ArgumentException ex)
            {
                // Error de validación (Ej: No envió ni email ni documento)
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Error de negocio (Ej: El correo ya existe)
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var response = await _authService.LoginAsync(dto);

                // Retorna 200 OK con el token
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Credenciales incorrectas o cuenta inactiva
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Cualquier otro error inesperado
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}