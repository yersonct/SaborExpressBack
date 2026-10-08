using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Exceptions;
using SaborExpress.Modules.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly IEmailConfirmationService _emailConfirmationService;
        private readonly IEmployeeActivationService _employeeActivationService;

        public AuthController(
            IAuthService authService,
            IPasswordResetService passwordResetService,
            IEmailConfirmationService emailConfirmationService,
            IEmployeeActivationService employeeActivationService)
        {
            _authService = authService;
            _passwordResetService = passwordResetService;
            _emailConfirmationService = emailConfirmationService;
            _employeeActivationService = employeeActivationService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var response = await _authService.LoginAsync(dto);
            return Ok(response);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _passwordResetService.ForgotPasswordAsync(dto);
            return Ok(new { message = "Se ha enviado un codigo de recuperacion a tu correo." });
        }

        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto dto)
        {
            var response = await _passwordResetService.VerifyResetCodeAsync(dto);
            return Ok(response);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _passwordResetService.ResetPasswordAsync(dto);
            return Ok(new { message = "Contraseña actualizada correctamente." });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Sesion cerrada correctamente." });
        }


        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            await _emailConfirmationService.ConfirmEmailAsync(dto);
            return Ok(new { message = "Correo confirmado correctamente. Ya puedes iniciar sesion." });
        }

        [HttpPost("resend-confirmation-code")]
        public async Task<IActionResult> ResendConfirmationCode([FromBody] ResendConfirmationDto dto)
        {
            await _emailConfirmationService.ResendConfirmationCodeAsync(dto);
            return Ok(new { message = "Si el correo existe y no ha sido confirmado, se enviara un nuevo codigo." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var response = await _authService.RefreshTokenAsync(dto);
            return Ok(response);
        }
        [HttpPost("resend-activation-code")]
        public async Task<IActionResult> ResendActivationCode([FromBody] ResendActivationDto dto)
        {
            await _employeeActivationService.ResendActivationCodeAsync(dto);
            return Ok(new { message = "Si la cuenta existe y no ha sido activada, se enviara un nuevo codigo." });
        }

        [HttpPost("activate-account")]
        public async Task<IActionResult> ActivateAccount([FromBody] ActivateAccountDto dto)
        {
            await _employeeActivationService.ActivateAccountAsync(dto);
            return Ok(new { message = "Cuenta activada correctamente. Ya puedes iniciar sesion con tu nueva contrasena." });
        }
    }
}
