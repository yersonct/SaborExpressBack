using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;

using SaborExpress.Shared.Interfaces;
using SaborExpress.Shared.Helpers;

namespace SaborExpress.Modules.Auth.Services
{
    public class EmailConfirmationService : VerificationCodeServiceBase<EmailConfirmationCode>, IEmailConfirmationService
    {
        private const int CodeExpirationMinutes = 60;

        private readonly IAuthRepository _authRepository;
        private readonly IEmailConfirmationRepository _confirmationRepository;
        private readonly INotificationService _notificationService;

        private readonly IBackgroundEmailQueue _emailQueue;
        private readonly IConfiguration _configuration;

        public EmailConfirmationService(
            IAuthRepository authRepository,
            IEmailConfirmationRepository confirmationRepository,
            INotificationService notificationService,
            IBackgroundEmailQueue emailQueue,
            IConfiguration configuration)
        {
            _authRepository = authRepository;
            _confirmationRepository = confirmationRepository;
            _notificationService = notificationService;
            _emailQueue = emailQueue;
            _configuration = configuration;
        }

        public async Task SendConfirmationCodeAsync(User user)
        {
            await _confirmationRepository.InvalidatePendingCodesAsync(user.Id);

            var code = ActivationTokenGenerator.Generate();

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
            var baseUrl = (_configuration["App:FrontendBaseUrl"] ?? "").TrimEnd('/');
            var link = $"{baseUrl}/confirm-email?token={Uri.EscapeDataString(code)}";
            var email = user.Email;

            await _emailQueue.QueueEmailAsync(ct => _notificationService.SendEmailAsync(
                email,
                "Confirma tu correo - SaborExpress",
                "Gracias por registrarte en SaborExpress.<br>" +
                "Presiona el boton para activar tu cuenta." +
                EmailButtonHelper.Build("Activar cuenta", link) +
                $"El enlace expira en {CodeExpirationMinutes} minutos."));
        }

        public async Task ConfirmEmailAsync(ConfirmEmailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                throw new UnauthorizedAccessException("Enlace invalido o expirado.");

            var code = await _confirmationRepository.GetByTokenAsync(dto.Token.Trim())
                ?? throw new UnauthorizedAccessException("Enlace invalido o expirado.");

            var user = code.User;

            if (user.EmailConfirmed)
                return; // ya activada: abrir el enlace dos veces no da error

            if (code.IsUsed || code.CodeExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Enlace invalido o expirado.");

            code.IsUsed = true;
            await _confirmationRepository.UpdateAsync(code);

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