// Modules/Auth/Services/EmployeeActivationService.cs
using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Auth.Validators;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Shared.Helpers;
namespace SaborExpress.Modules.Auth.Services
{
    public class EmployeeActivationService : VerificationCodeServiceBase<EmployeeActivationCode>, IEmployeeActivationService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IEmployeeActivationRepository _activationRepository;
        private readonly INotificationService _notificationService;
        private readonly ActivateAccountValidator _validator;
        private readonly IConfiguration _configuration;


        private const int CodeExpirationMinutes = 1440;

        public EmployeeActivationService(
            IAuthRepository authRepository,
            IEmployeeActivationRepository activationRepository,
            INotificationService notificationService,
            ActivateAccountValidator validator,
            IConfiguration configuration)
        {
            _authRepository = authRepository;
            _activationRepository = activationRepository;
            _notificationService = notificationService;
            _validator = validator;
            _configuration = configuration;
        }

        public async Task SendActivationCodeAsync(int userId, string email, string employeeName)
        {
            var code = ActivationTokenGenerator.Generate();

            await _activationRepository.InvalidatePendingAsync(userId); 

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

                var baseUrl = (_configuration["App:FrontendBaseUrl"] ?? "").TrimEnd('/');
                var link = $"{baseUrl}/activate?token={Uri.EscapeDataString(code)}";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendEmailAsync(
                            email,
                            "Bienvenido a SaborExpress - Activa tu cuenta",
                            $"Hola {employeeName}, se creo tu cuenta de empleado en SaborExpress.<br>" +
                            "Presiona el boton para activar tu cuenta y definir tu propia contrasena." +
                            EmailButtonHelper.Build("Activar cuenta", link) +
                            "El enlace expira en 24 horas."
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

            var activationCode = await _activationRepository.GetByTokenAsync(dto.Token.Trim())
                ?? throw new UnauthorizedAccessException("Enlace invalido o expirado.");

            if (activationCode.IsUsed || activationCode.CodeExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Enlace invalido o expirado.");

            var user = activationCode.User;
            activationCode.IsUsed = true;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.Status = true;
            user.MustChangePassword = false;
            // El código de activación ya prueba que el empleado recibió el correo,
            // así que no hace falta pedirle además que "confirme" el email por separado.
            user.EmailConfirmed = true;

            await _authRepository.UpdateAsync(user);
            await _activationRepository.UpdateAsync(activationCode);
            await _activationRepository.SaveChangesAsync();
        }
        public async Task ResendActivationCodeAsync(ResendActivationDto dto)
        {
            var user = await _authRepository.GetByIdentifierAsync(dto.Identifier);

            // No revela si el correo existe, ya está activo, o no es de un empleado
            // (mismo criterio de seguridad que ya usas en confirmación de email).
            if (user == null || user.Employee == null || user.Status)
                return;

            await SendActivationCodeAsync(user.Id, user.Email!, user.Employee.Name);
        }
    }
}