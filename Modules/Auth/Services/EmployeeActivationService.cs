// Modules/Auth/Services/EmployeeActivationService.cs
using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Auth.Validators;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.Auth.Services
{
    public class EmployeeActivationService : VerificationCodeServiceBase<EmployeeActivationCode>, IEmployeeActivationService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IEmployeeActivationRepository _activationRepository;
        private readonly INotificationService _notificationService;
        private readonly ActivateAccountValidator _validator;

        private const int CodeExpirationMinutes = 30;

        public EmployeeActivationService(
            IAuthRepository authRepository,
            IEmployeeActivationRepository activationRepository,
            INotificationService notificationService,
            ActivateAccountValidator validator)
        {
            _authRepository = authRepository;
            _activationRepository = activationRepository;
            _notificationService = notificationService;
            _validator = validator;
        }

        public async Task SendActivationCodeAsync(int userId, string email, string employeeName)
        {
            var code = ResetCodeGenerator.GenerateSixDigitCode();

            var activationCode = new EmployeeActivationCode
            {
                UserId = userId,
                Code = code,
                CodeExpiresAt = DateTime.UtcNow.AddMinutes(CodeExpirationMinutes),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _activationRepository.AddAsync(activationCode);
            await _activationRepository.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendEmailAsync(
                        email,
                        "Bienvenido a SaborExpress - Activa tu cuenta",
                        $"Hola {employeeName}, se creo tu cuenta de empleado en SaborExpress.<br>" +
                        $"Tu codigo de activacion es: <b>{code}</b>. Expira en {CodeExpirationMinutes} minutos.<br>" +
                        $"Ingresa a la app con tu correo y ese codigo para definir tu propia contrasena."
                    );
                }
                catch
                {
                    // No interrumpe la creacion del empleado
                }
            });
        }

        public async Task ActivateAccountAsync(ActivateAccountDto dto)
        {
            _validator.Validate(dto);

            var user = await _authRepository.GetByIdentifierAsync(dto.Identifier)
                ?? throw new UnauthorizedAccessException("Codigo invalido o expirado.");

            var activationCode = await ValidateAndConsumeCodeAsync(
                getLatestCode: () => _activationRepository.GetLatestByUserIdAsync(user.Id),
                updateCode: async code =>
                {
                    await _activationRepository.UpdateAsync(code);
                    await _activationRepository.SaveChangesAsync();
                },
                inputCode: dto.Code
            );

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.Status = true;
            user.MustChangePassword = false;

            await _authRepository.UpdateAsync(user);
            await _activationRepository.UpdateAsync(activationCode);
            await _activationRepository.SaveChangesAsync();
        }
    }
}