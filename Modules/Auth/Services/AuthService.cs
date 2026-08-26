using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Mappings;
using SaborExpress.Modules.Auth.Validators;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Interfaces;
using Microsoft.Extensions.Options;
using SaborExpress.Configuration;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Auth.Exceptions;

namespace SaborExpress.Modules.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly LoginValidator _loginValidator;
        private readonly JwtTokenGenerator _jwtGenerator;
        private readonly IOptions<JwtSettings> _jwtSettings;
        private readonly INotificationService _notificationService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthService(
            IAuthRepository authRepository,
            LoginValidator loginValidator,
            JwtTokenGenerator jwtGenerator,
            IOptions<JwtSettings> jwtSettings,
            INotificationService notificationService,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _authRepository = authRepository;
            _loginValidator = loginValidator;
            _jwtGenerator = jwtGenerator;
            _jwtSettings = jwtSettings;
            _notificationService = notificationService;
            _refreshTokenRepository = refreshTokenRepository;
        }

        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 10;

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _authRepository.GetByIdentifierAsync(dto.Identifier);

        if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            var retryAfter = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalSeconds;
            throw new TooManyLoginAttemptsException(retryAfter);
        }

        var esValido = user != null && BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!esValido)
        {
            if (user != null)
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                    user.FailedLoginAttempts = 0;
                }

                await _authRepository.UpdateAsync(user);
            }

            throw new UnauthorizedAccessException("Credenciales incorrectas.");
        }

        // A partir de acá sabemos que la contraseña es correcta.
        // Solo falta validar el resto de reglas de negocio (cuenta activa, email confirmado, etc.)
        _loginValidator.ValidateAccountRules(user!, dto);

        user!.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        var token = _jwtGenerator.Generate(user, dto.Identifier);
        user.Token = token;
        user.LastLogin = DateTime.UtcNow;
        await _authRepository.UpdateAsync(user);

        var nombreCompleto = user.GetDisplayName(dto.Identifier);
        if (!string.IsNullOrEmpty(user.Email))
        {
            NotifyLoginAsync(user.Email, nombreCompleto);
        }

        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);
        return AuthMapper.ToAuthResponse(user, token, refreshToken);
    }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(int userId)
        {
            var refreshToken = RefreshTokenGenerator.Generate();

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(_jwtSettings.Value.RefreshTokenExpirationHours)
            });

            return refreshToken;
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            var stored = await _refreshTokenRepository.GetActiveByTokenAsync(dto.RefreshToken)
                ?? throw new UnauthorizedAccessException("Refresh token invalido o expirado.");

            var user = stored.User;

            if (!user.Status)
                throw new UnauthorizedAccessException("La cuenta esta deshabilitada.");

            await _refreshTokenRepository.RevokeAsync(stored);

            var newAccessToken = _jwtGenerator.Generate(user, user.Email!);
            user.Token = newAccessToken;
            await _authRepository.UpdateAsync(user);

            var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);

            return AuthMapper.ToAuthResponse(user, newAccessToken, newRefreshToken);
        }

        private void NotifyLoginAsync(string email, string nombreCompleto)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendEmailAsync(
                        email,
                        "Inicio de Sesion - SaborExpress",
                        $"El usuario '{nombreCompleto}' ha iniciado sesion en SaborExpress.");
                }
                catch
                {
                    // No interrumpe el login: solo se registra si falla el envio.
                    // TODO: reemplazar por ILogger cuando se centralice el logging del proyecto.
                }
            });
        }

        public async Task LogoutAsync(int userId)
        {
            var user = await _authRepository.GetByIdWithRelationsAsync(userId);
            if (user == null)
                return;

            user.Token = null;
            await _authRepository.UpdateAsync(user);
            await _refreshTokenRepository.RevokeAllForUserAsync(userId);
        }
    }
}
