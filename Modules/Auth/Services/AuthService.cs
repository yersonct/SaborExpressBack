    using System.Collections.Concurrent;
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
    using SaborExpress.Modules.Notifications.Interfaces;
    using SaborExpress.Modules.Branches.Interfaces;
    using SaborExpress.Data;
    using Microsoft.EntityFrameworkCore;
    using SaborExpress.Shared.Helpers;

    namespace SaborExpress.Modules.Auth.Services
    {
        public class AuthService : IAuthService
        {
            private readonly IAuthRepository _authRepository;
            private readonly LoginValidator _loginValidator;
            private readonly JwtTokenGenerator _jwtGenerator;
            private readonly IOptions<JwtSettings> _jwtSettings;
            private readonly INotificationService _notificationService;
        private readonly IRealtimeNotifier _realtimeNotifier;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IEmployeeScheduleRepository _scheduleRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IConfiguration _configuration;
        private readonly IBranchRepository _branchRepository;
        private readonly AppDbContext _db;

        // Serializa login y renovación para que un refresh antiguo no sustituya la sesión nueva.
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> SessionLocks = new();

        private static readonly string[] OperationalRoles =
        {
            RoleNames.Mesero, RoleNames.Cajero, RoleNames.Cocinero, RoleNames.AuxiliarCocina, RoleNames.Repartidor
        };

        private static readonly string[] WebPanelRoles =
        {
            RoleNames.Gerente, RoleNames.Administrador
        };

            public AuthService(
                IAuthRepository authRepository,
                LoginValidator loginValidator,
                JwtTokenGenerator jwtGenerator,
                IOptions<JwtSettings> jwtSettings,
                INotificationService notificationService,
                IRealtimeNotifier realtimeNotifier,
                IRefreshTokenRepository refreshTokenRepository,
                IEmployeeScheduleRepository scheduleRepository,
                ICustomerRepository customerRepository,
                INotificationRepository notificationRepository,
                IConfiguration configuration,
                IBranchRepository branchRepository,
                AppDbContext db)
                {
                    _db = db;
                    _branchRepository = branchRepository;
                    _configuration = configuration;
                    _customerRepository = customerRepository;
                    _notificationRepository = notificationRepository;
                _authRepository = authRepository;
                _loginValidator = loginValidator;
                _jwtGenerator = jwtGenerator;
                _jwtSettings = jwtSettings;
                _notificationService = notificationService;
                _realtimeNotifier = realtimeNotifier;
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

            await EnsureBranchIsActiveAsync(user);

            var sessionLock = SessionLocks.GetOrAdd(user.Id, static _ => new SemaphoreSlim(1, 1));
            await sessionLock.WaitAsync();
            try
            {
                var enforceSingleSession = _configuration.GetValue("Auth:SingleSession", true);
                await EnsureCustomerForEmployeeAsync(user);

                var effective = await EffectiveRolePolicy.ResolveAsync(user, _scheduleRepository);
                var hasActiveShift = effective.HasActiveShift;
                var activeShiftRoleName = effective.ActiveShiftRoleName;
                var effectiveRoles = effective.Roles;
                var token = _jwtGenerator.Generate(user, dto.Identifier,
                    actAsClient: !hasActiveShift, effectiveRoles: effectiveRoles);
                user.Token = token;
                user.LastLogin = DateTime.UtcNow;
                await _authRepository.UpdateAsync(user);

                // Retira los destinos push anteriores y revoca las sesiones previas.
                if (enforceSingleSession)
                    await _notificationRepository.RemovePushDevicesByUserIdAsync(user.Id);

                var nombreCompleto = user.GetDisplayName(dto.Identifier);
                if (!string.IsNullOrEmpty(user.Email))
                    NotifyLoginAsync(user.Email, nombreCompleto);

                if (enforceSingleSession)
                    await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);

                var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);
                if (enforceSingleSession)
                    await _realtimeNotifier.SessionReplacedAsync(user.Id);

                return AuthMapper.ToAuthResponse(user, token, refreshToken, hasActiveShift, activeShiftRoleName, effectiveRoles);
            }
            finally
            {
                sessionLock.Release();
            }
        }
        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                throw new ArgumentException("La contraseña actual es obligatoria.");

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
                throw new ArgumentException("La nueva contraseña debe tener al menos 8 caracteres.");

            if (!dto.NewPassword.Any(char.IsLetter) || !dto.NewPassword.Any(char.IsDigit))
                throw new ArgumentException("La nueva contraseña debe incluir al menos una letra y un número.");

            if (dto.ConfirmPassword is not null && dto.ConfirmPassword != dto.NewPassword)
                throw new ArgumentException("Las contraseñas no coinciden.");

            var user = await _authRepository.GetByIdWithRelationsAsync(userId)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new ArgumentException("La contraseña actual no es correcta.");

            if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash))
                throw new ArgumentException("La nueva contraseña debe ser diferente a la actual.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.MustChangePassword = false;
            user.Token = null; // invalida el access token actual
            await _authRepository.UpdateAsync(user);

            // Ninguna sesión previa puede renovarse con la contraseña vieja.
            await _refreshTokenRepository.RevokeAllForUserAsync(userId);
        }

        // Con la sede desactivada solo entran Gerente y Administrador (panel web).
        private async Task EnsureBranchIsActiveAsync(User user)
        {
            if (user.Employee?.BranchId is not int branchId) return;

            if (user.UserRoles.Any(ur => WebPanelRoles.Contains(ur.Role.Name))) return;

            // No se emiten ni renuevan sesiones operativas luego del cierre
            // de la sede. La regla expira automáticamente al cambiar de día
            // en zona horaria Colombia.
            var today = DateOnly.FromDateTime(ColombiaTime.Now);
            if (await _db.CashClosures.AsNoTracking().AnyAsync(c =>
                c.BranchId == branchId && c.ShiftDate == today && c.ReopenedAtUtc == null))
                throw new UnauthorizedAccessException(
                    "La caja de hoy ya fue cerrada. Podrás ingresar en tu próximo turno, otro día.");

            var branch = await _branchRepository.GetByIdAsync(branchId);
            if (branch is { Status: false })
                throw new UnauthorizedAccessException("Tu sede está desactivada. Contacta al gerente.");
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

        // Los roles efectivos se resuelven de forma centralizada en EffectiveRolePolicy.

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

                var sessionLock = SessionLocks.GetOrAdd(stored.UserId, static _ => new SemaphoreSlim(1, 1));
                await sessionLock.WaitAsync();
                try
                {
                    // El login pudo revocar este token mientras esperábamos el bloqueo.
                    stored = await _refreshTokenRepository.GetActiveByTokenAsync(dto.RefreshToken)
                        ?? throw new UnauthorizedAccessException("Refresh token invalido o expirado.");

                    // Se recarga el usuario con roles, Employee y Customer para conservar sus claims.
                    var user = await _authRepository.GetByIdWithRelationsAsync(stored.UserId)
                        ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

                    if (!user.Status)
                        throw new UnauthorizedAccessException("La cuenta esta deshabilitada.");

                    await EnsureBranchIsActiveAsync(user);
                    await EnsureCustomerForEmployeeAsync(user);
                    await _refreshTokenRepository.RevokeAsync(stored);

                    var effective = await EffectiveRolePolicy.ResolveAsync(user, _scheduleRepository);
                    var hasActiveShift = effective.HasActiveShift;
                    var activeShiftRoleName = effective.ActiveShiftRoleName;
                    var effectiveRoles = effective.Roles;
                    var newAccessToken = _jwtGenerator.Generate(user, user.Email!,
                        actAsClient: !hasActiveShift, effectiveRoles: effectiveRoles);
                    user.Token = newAccessToken;
                    await _authRepository.UpdateAsync(user);

                    var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);
                    return AuthMapper.ToAuthResponse(user, newAccessToken, newRefreshToken, hasActiveShift, activeShiftRoleName, effectiveRoles);
                }
                finally
                {
                    sessionLock.Release();
                }
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
