using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Exceptions;
using SaborExpress.Modules.Auth.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SaborExpress.Shared.Constants;
using SaborExpress.Configuration;
using Microsoft.Extensions.Options;

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
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            IAuthService authService,
            IPasswordResetService passwordResetService,
            IEmailConfirmationService emailConfirmationService,
            IEmployeeActivationService employeeActivationService,
            IConfiguration configuration,
            IHostEnvironment hostEnvironment,
            IOptions<JwtSettings> jwtSettings)
        {
            _authService = authService;
            _passwordResetService = passwordResetService;
            _emailConfirmationService = emailConfirmationService;
            _employeeActivationService = employeeActivationService;
            _configuration = configuration;
            _hostEnvironment = hostEnvironment;
            _jwtSettings = jwtSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var response = await _authService.LoginAsync(dto);
            if (IsAdminWebClient())
            {
                SetSessionCookies(response);
                response.Token = string.Empty;
                response.RefreshToken = string.Empty;
            }
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _passwordResetService.ForgotPasswordAsync(dto);
            return Ok(new { message = "Si la cuenta esta habilitada para recuperacion, recibiras un codigo." });
        }

        [AllowAnonymous]
        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto dto)
        {
            var response = await _passwordResetService.VerifyResetCodeAsync(dto);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _passwordResetService.ResetPasswordAsync(dto);
            return Ok(new { message = "Contraseña actualizada correctamente." });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            await _authService.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Contraseña actualizada correctamente." });
        }

        [Authorize]
        [HttpGet("session")]
        public IActionResult ValidateSession()
        {
            if (!IsAdminWebClient())
                return NoContent();

            return Ok(GetSafeSessionResponse());
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            await _authService.LogoutAsync(userId);
            if (IsAdminWebClient())
                ClearSessionCookies();
            return Ok(new { message = "Sesion cerrada correctamente." });
        }


        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            await _emailConfirmationService.ConfirmEmailAsync(dto);
            return Ok(new { message = "Correo confirmado correctamente. Ya puedes iniciar sesion." });
        }

        [AllowAnonymous]
        [HttpPost("resend-confirmation-code")]
        public async Task<IActionResult> ResendConfirmationCode([FromBody] ResendConfirmationDto dto)
        {
            await _emailConfirmationService.ResendConfirmationCodeAsync(dto);
            return Ok(new { message = "Si el correo existe y no ha sido confirmado, se enviara un nuevo codigo." });
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var response = await _authService.RefreshTokenAsync(dto);
            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("web-refresh-token")]
        public async Task<IActionResult> WebRefreshToken()
        {
            if (!IsAdminWebClient())
                return Forbid();

            var refreshToken = Request.Cookies[AuthCookieNames.RefreshToken];
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized();

            var response = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto
            {
                RefreshToken = refreshToken
            });
            SetSessionCookies(response);
            response.Token = string.Empty;
            response.RefreshToken = string.Empty;
            return Ok(response);
        }
        [AllowAnonymous]
        [HttpPost("resend-activation-code")]
        public async Task<IActionResult> ResendActivationCode([FromBody] ResendActivationDto dto)
        {
            await _employeeActivationService.ResendActivationCodeAsync(dto);
            return Ok(new { message = "Si la cuenta existe y no ha sido activada, se enviara un nuevo codigo." });
        }

        [AllowAnonymous]
        [HttpPost("activate-account")]
        public async Task<IActionResult> ActivateAccount([FromBody] ActivateAccountDto dto)
        {
            await _employeeActivationService.ActivateAccountAsync(dto);
            return Ok(new { message = "Cuenta activada correctamente. Ya puedes iniciar sesion con tu nueva contrasena." });
        }

        private bool IsAdminWebClient() =>
            string.Equals(Request.Headers[AuthCookieNames.WebClientHeader],
                AuthCookieNames.WebClientValue, StringComparison.Ordinal);

        private object GetSafeSessionResponse()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return new
            {
                userId = int.TryParse(userId, out var parsedUserId) ? parsedUserId : 0,
                identifier = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                employeeId = int.TryParse(User.FindFirstValue("EmployeeId"), out var parsedEmployeeId)
                    ? parsedEmployeeId
                    : (int?)null,
                roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
            };
        }

        private void SetSessionCookies(AuthResponseDto response)
        {
            var options = CreateCookieOptions("/");
            options.Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);
            Response.Cookies.Append(AuthCookieNames.AccessToken, response.Token, options);

            var refreshOptions = CreateCookieOptions("/api/Auth/web-refresh-token");
            refreshOptions.Expires = DateTimeOffset.UtcNow.AddHours(_jwtSettings.RefreshTokenExpirationHours);
            Response.Cookies.Append(AuthCookieNames.RefreshToken, response.RefreshToken, refreshOptions);
        }

        private void ClearSessionCookies()
        {
            Response.Cookies.Delete(AuthCookieNames.AccessToken, CreateCookieOptions("/"));
            Response.Cookies.Delete(AuthCookieNames.RefreshToken,
                CreateCookieOptions("/api/Auth/web-refresh-token"));
        }

        private CookieOptions CreateCookieOptions(string path)
        {
            var sameSiteSetting = _configuration["Auth:Cookies:SameSite"];
            if (!Enum.TryParse<SameSiteMode>(sameSiteSetting, ignoreCase: true, out var sameSite))
                sameSite = SameSiteMode.Lax;

            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !_hostEnvironment.IsDevelopment() || Request.IsHttps,
                SameSite = sameSite,
                Path = path,
                Domain = _configuration["Auth:Cookies:Domain"],
                IsEssential = true
            };
        }
    }
}
