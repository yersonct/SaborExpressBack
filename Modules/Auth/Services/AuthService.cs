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

    using SaborExpress.Modules.EmployeeSchedules.Interfaces;
    using SaborExpress.Modules.EmployeeSchedules.Enum;
    using SaborExpress.Shared.Constants;
    using SaborExpress.Modules.Customers.Interfaces;
    using SaborExpress.Modules.Customers.Models;

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
        private readonly IEmployeeScheduleRepository _scheduleRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IConfiguration _configuration;

        private static readonly string[] OperationalRoles =
        {
            RoleNames.Mesero, RoleNames.Cajero, RoleNames.Cocinero, RoleNames.Repartidor
        };

            public AuthService(
                IAuthRepository authRepository,
                LoginValidator loginValidator,
                JwtTokenGenerator jwtGenerator,
                IOptions<JwtSettings> jwtSettings,
                INotificationService notificationService,
                IRefreshTokenRepository refreshTokenRepository,
                IEmployeeScheduleRepository scheduleRepository,
                ICustomerRepository customerRepository,
                IConfiguration configuration)
                {
                    _configuration = configuration;
                    _customerRepository = customerRepository;
                _authRepository = authRepository;
                _loginValidator = loginValidator;
                _jwtGenerator = jwtGenerator;
                _jwtSettings = jwtSettings;
                _notificationService = notificationService;
                _refreshTokenRepository = refreshTokenRepository;
                _scheduleRepository = scheduleRepository;

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

            if (user != null)
            {
                // Primero revisamos si la cuenta está en un estado especial
                // (no activada, email sin confirmar, retirado, etc.) ANTES de
                // comparar la contraseña, porque cuentas de empleado recién
                // creadas tienen un PasswordHash aleatorio que nunca va a coincidir.
                _loginValidator.ValidateAccountRules(user, dto);
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

            user!.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            await EnsureCustomerForEmployeeAsync(user);

            var token = _jwtGenerator.Generate(user, dto.Identifier);
            user.Token = token;
            user.LastLogin = DateTime.UtcNow;
            await _authRepository.UpdateAsync(user);

            var nombreCompleto = user.GetDisplayName(dto.Identifier);
            if (!string.IsNullOrEmpty(user.Email))
            {
                NotifyLoginAsync(user.Email, nombreCompleto);
            }

            // 👇 nuevo: sesión única — cualquier dispositivo anterior con este
            // usuario deja de poder refrescar su token en cuanto expire.
            if (_configuration.GetValue("Auth:SingleSession", true))
                await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);

        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);
        var (hasActiveShift, activeShiftRoleName) = await GetActiveShiftInfoAsync(user);
        return AuthMapper.ToAuthResponse(user, token, refreshToken, hasActiveShift, activeShiftRoleName);
    }

        // Un empleado operativo (Mesero, Cajero, Cocinero, Repartidor) puede usar la app
        // como cliente cuando no tiene turno. Direcciones y pedidos necesitan un Customer
        // real, así que se crea una vez, copiando sus datos de Employee.
        private async Task EnsureCustomerForEmployeeAsync(User user)
        {
            if (user.Customer != null || user.Employee == null) return;

            var esOperativo = user.UserRoles.Any(ur => OperationalRoles.Contains(ur.Role.Name));
            if (!esOperativo) return;

            var customer = new Customer
            {
                UserId = user.Id,
                Name = user.Employee.Name,
                LastName = user.Employee.LastName,
                Document = user.Employee.Document,
                Phone = user.Employee.Phone,
                Address = user.Employee.Address
            };

            await _customerRepository.AddAsync(customer);
            user.Customer = customer; // para que el token lo incluya
        }

        private async Task<(bool HasActiveShift, string? ActiveShiftRoleName)> GetActiveShiftInfoAsync(User user)
        {
            // Cliente, Gerente y Administrador no dependen de turno.
            var esOperativo = user.UserRoles.Any(ur => OperationalRoles.Contains(ur.Role.Name));
            if (!esOperativo || user.Employee == null)
                return (true, null);

            var nowColombia = SaborExpress.Shared.Helpers.ColombiaTime.Now;
            var today = DateOnly.FromDateTime(nowColombia);
            var now = TimeOnly.FromDateTime(nowColombia);

        var shiftsToday = await _scheduleRepository.GetByEmployeeAndDateAsync(user.Employee.Id, today);

        var activeShift = shiftsToday.FirstOrDefault(s =>
            s.Status == ScheduleStatus.Programado &&
            now >= s.StartTime &&
            now <= s.EndTime);

        if (activeShift == null)
            return (false, null);

        // Resolvemos el nombre del rol desde los UserRoles ya cargados en 'user',
        // haciendo match por RoleId — evita otro query o Include extra.
        var roleName = user.UserRoles.FirstOrDefault(ur => ur.RoleId == activeShift.RoleId)?.Role.Name;

        return (true, roleName);
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

                // Se recarga el usuario con roles, Employee y Customer: stored.User puede venir
                // sin esas relaciones y el token renovado perdería CustomerId/EmployeeId.
                var user = await _authRepository.GetByIdWithRelationsAsync(stored.UserId)
                    ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

                if (!user.Status)
                    throw new UnauthorizedAccessException("La cuenta esta deshabilitada.");

                await EnsureCustomerForEmployeeAsync(user);

                await _refreshTokenRepository.RevokeAsync(stored);

                var newAccessToken = _jwtGenerator.Generate(user, user.Email!);
                user.Token = newAccessToken;
                await _authRepository.UpdateAsync(user);

                var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);
                var (hasActiveShift, activeShiftRoleName) = await GetActiveShiftInfoAsync(user);

                return AuthMapper.ToAuthResponse(user, newAccessToken, newRefreshToken, hasActiveShift, activeShiftRoleName);
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
