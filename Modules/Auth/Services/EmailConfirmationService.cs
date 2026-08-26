using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Shared.Interfaces;


namespace SaborExpress.Modules.Auth.Services
{
    public class EmailConfirmationService : VerificationCodeServiceBase<EmailConfirmationCode>, IEmailConfirmationService
    {
        private const int CodeExpirationMinutes = 10;

        private readonly IAuthRepository _authRepository;
        private readonly IEmailConfirmationRepository _confirmationRepository;
        private readonly INotificationService _notificationService;

        private readonly IBackgroundEmailQueue _emailQueue;    

        public EmailConfirmationService(
            IAuthRepository authRepository,
            IEmailConfirmationRepository confirmationRepository,
            INotificationService notificationService,
            IBackgroundEmailQueue emailQueue)
        {
            _authRepository = authRepository;
            _confirmationRepository = confirmationRepository;
            _notificationService = notificationService;
            _emailQueue = emailQueue;           
        }

        public async Task SendConfirmationCodeAsync(User user)
        {
            await _confirmationRepository.InvalidatePendingCodesAsync(user.Id);

            var code = ResetCodeGenerator.GenerateSixDigitCode();

            var confirmationCode = new EmailConfirmationCode
            {
                UserId = user.Id,
                Code = code,
                CodeExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpirationMinutes),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            if (string.IsNullOrEmpty(user.Email))
                throw new ArgumentException("El usuario no tiene un correo registrado");

            await _confirmationRepository.AddAsync(confirmationCode);
            await _emailQueue.QueueEmailAsync(ct => _notificationService.SendEmailAsync(
                user.Email,
                "Confirma tu correo - SaborExpress",
                $"Tu codigo de confirmacion es: <b>{code}</b>. Expira en {CodeExpirationMinutes} minutos."));
        }

        public async Task ConfirmEmailAsync(ConfirmEmailDto dto)
        {
            var user = await _authRepository.GetByIdentifierAsync(dto.Identifier)
                ?? throw new UnauthorizedAccessException("Codigo invalido o expirado.");

            if (user.EmailConfirmed)
                return;

            await ValidateAndConsumeCodeAsync(
                getLatestCode: () => _confirmationRepository.GetLatestCodeByUserIdAsync(user.Id),
                updateCode: code => _confirmationRepository.UpdateAsync(code),
                inputCode: dto.Code
            );

            user.EmailConfirmed = true;
            await _authRepository.UpdateAsync(user);
        }

        public async Task ResendConfirmationCodeAsync(ResendConfirmationDto dto)
        {
            var user = await _authRepository.GetByIdentifierAsync(dto.Identifier);

            if (user == null || user.EmailConfirmed)
                return; 

            await SendConfirmationCodeAsync(user);
        }
    }
}