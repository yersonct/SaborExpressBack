using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Auth.Validators;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Modules.Auth.Exceptions;

namespace SaborExpress.Modules.Auth.Services
{
    public class PasswordResetService : VerificationCodeServiceBase<PasswordResetCode>, IPasswordResetService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IPasswordResetRepository _resetRepository;
        private readonly INotificationService _notificationService;
        private readonly ForgotPasswordValidator _forgotPasswordValidator;
        private readonly VerifyResetCodeValidator _verifyResetCodeValidator;
        private readonly ResetPasswordValidator _resetPasswordValidator; 

        private readonly IBackgroundEmailQueue _emailQueue;

        private const int CodeExpirationMinutes = 5;
        private const int ResetTokenExpirationMinutes = 15;

        public PasswordResetService(
            IAuthRepository authRepository,
            IPasswordResetRepository resetRepository,
            INotificationService notificationService,
            IBackgroundEmailQueue emailQueue,
            ForgotPasswordValidator forgotPasswordValidator,
            VerifyResetCodeValidator verifyResetCodeValidator,
            ResetPasswordValidator resetPasswordValidator)
        {
            _authRepository = authRepository;
            _resetRepository = resetRepository;
            _notificationService = notificationService;
            _emailQueue = emailQueue; 
            _forgotPasswordValidator = forgotPasswordValidator;
            _verifyResetCodeValidator = verifyResetCodeValidator;
            _resetPasswordValidator = resetPasswordValidator;
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            await _forgotPasswordValidator.ValidateAsync(dto);

            var user = await _authRepository.GetByIdentifierAsync(dto.Identifier);

            if (user == null)
                return;

            var windowStart = DateTime.UtcNow.AddMinutes(-PasswordResetRateLimiter.WindowMinutes);
            var recentAttempts = await _resetRepository.GetRecentAttemptTimestampsAsync(user.Id, windowStart);

            var retryAfterSeconds = PasswordResetRateLimiter.GetRetryAfterSecondsIfLocked(recentAttempts);
            if (retryAfterSeconds.HasValue)
                throw new TooManyResetAttemptsException(retryAfterSeconds.Value);

            await _resetRepository.InvalidatePendingCodesAsync(user.Id);

            var code = ResetCodeGenerator.GenerateSixDigitCode();

            var resetCode = new PasswordResetCode
            {
                UserId = user.Id,
                Code = code,
                CodeExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpirationMinutes),
                IsUsed = false,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            if (string.IsNullOrEmpty(user.Email))
                throw new ArgumentException("El usuario no tiene un correo registrado");

            await _resetRepository.AddAsync(resetCode);
            var nombreCompleto = user.GetDisplayName(dto.Identifier);
            await _emailQueue.QueueEmailAsync(ct => _notificationService.SendEmailAsync(
                user.Email,
                "Recuperacion de Contraseña - SaborExpress",
                $"Hola {nombreCompleto}, tu codigo de recuperacion es: <b>{code}</b>. Expira en {CodeExpirationMinutes} minutos."
            ));
        }

        public async Task<VerifyResetCodeResponseDto> VerifyResetCodeAsync(VerifyResetCodeDto dto)
        {
            _verifyResetCodeValidator.Validate(dto);

            var user = await _authRepository.GetByIdentifierAsync(dto.Identifier);
            if (user == null)
                throw new UnauthorizedAccessException("Codigo invalido o expirado.");

            var resetCode = await ValidateAndConsumeCodeAsync(
                getLatestCode: () => _resetRepository.GetLatestCodeByUserIdAsync(user.Id),
                updateCode: code => _resetRepository.UpdateAsync(code),
                inputCode: dto.Code
            );

            resetCode.ResetToken = Guid.NewGuid().ToString("N");
            resetCode.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(ResetTokenExpirationMinutes);

            await _resetRepository.UpdateAsync(resetCode);

            return new VerifyResetCodeResponseDto
            {
                ResetToken = resetCode.ResetToken,
                ExpiresInMinutes = ResetTokenExpirationMinutes
            };
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            _resetPasswordValidator.Validate(dto);

            var resetCode = await _resetRepository.GetByResetTokenAsync(dto.ResetToken);
            if (resetCode == null)
                throw new UnauthorizedAccessException("El codigo de recuperacion es invalido o ha expirado.");

            var user = resetCode.User;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.Token = null;

            await _authRepository.UpdateAsync(user);

            resetCode.IsCompleted = true;
            await _resetRepository.UpdateAsync(resetCode);
        }
    }
}   