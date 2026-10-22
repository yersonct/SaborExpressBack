using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SaborExpress.Configuration
{
    public static class JwtConfiguration
    {
        public static IServiceCollection AddJwtConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            var secretKey = ObtenerSecretKey(configuration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = ConstruirParametrosValidacion(configuration, secretKey);
                    options.Events = ConstruirEventos();
                });

            return services;
        }

        private static string ObtenerSecretKey(IConfiguration configuration)
        {
            var secretKey = configuration["JwtSettings:SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException(
                    "JwtSettings:SecretKey no está configurado en User Secrets o appsettings");

            return secretKey;
        }

        private static TokenValidationParameters ConstruirParametrosValidacion(
            IConfiguration configuration, string secretKey)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = configuration["JwtSettings:Issuer"],
                ValidAudience = configuration["JwtSettings:Audience"],

                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

                ClockSkew = TimeSpan.Zero
            };
        }

        private static JwtBearerEvents ConstruirEventos()
        {
            return new JwtBearerEvents
            {
                // Nuevo: permite que el JWT viaje por query string, necesario
                // para que el front pueda conectarse a /hubs/notifications
                // (SignalR/WebSockets no permite headers personalizados).
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    else if (string.IsNullOrEmpty(context.Request.Headers.Authorization) &&
                        (string.Equals(context.Request.Headers[AuthCookieNames.WebClientHeader],
                            AuthCookieNames.WebClientValue, StringComparison.Ordinal) ||
                         path.StartsWithSegments("/hubs")))
                    {
                        // Cookies authenticate browser WebSocket connections without an
                        // Authorization header. Check their Origin explicitly because
                        // browsers do not apply CORS preflight to WebSocket upgrades.
                        if (path.StartsWithSegments("/hubs") && string.IsNullOrEmpty(accessToken))
                        {
                            var services = context.HttpContext.RequestServices;
                            var configuration = services.GetRequiredService<IConfiguration>();
                            var environment = services.GetRequiredService<IHostEnvironment>();
                            var allowedOrigins = CorsOriginPolicy.FilterConfiguredOrigins(
                                configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [],
                                environment.IsDevelopment());
                            if (!CorsOriginPolicy.IsAllowedOrigin(
                                context.Request.Headers.Origin.ToString(),
                                allowedOrigins,
                                environment.IsDevelopment()))
                            {
                                return Task.CompletedTask;
                            }
                        }

                        context.Token = context.Request.Cookies[AuthCookieNames.AccessToken];
                    }

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
                    {
                        context.Fail("Token inválido: no contiene el identificador del usuario.");
                        return;
                    }

                    var authRepository = context.HttpContext.RequestServices
                        .GetRequiredService<IAuthRepository>();

                    var (exists, dbToken) = await authRepository.GetSessionInfoAsync(userId);

                    if (!exists)
                    {
                        context.Fail("Usuario no encontrado.");
                        return;
                    }

                    // El token puede venir del header normal (requests HTTP comunes)
                    // o del query string (conexión a SignalR).
                    var rawToken = context.HttpContext.Request.Headers.Authorization
                        .ToString()
                        .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

                    if (string.IsNullOrEmpty(rawToken))
                        rawToken = context.HttpContext.Request.Query["access_token"].ToString();
                    if (string.IsNullOrEmpty(rawToken) &&
                        (string.Equals(context.HttpContext.Request.Headers[AuthCookieNames.WebClientHeader],
                            AuthCookieNames.WebClientValue, StringComparison.Ordinal) ||
                         context.HttpContext.Request.Path.StartsWithSegments("/hubs")))
                        rawToken = context.HttpContext.Request.Cookies[AuthCookieNames.AccessToken] ?? string.Empty;

                    var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                    var singleSession = config.GetValue("Auth:SingleSession", true);

                    if (singleSession && (string.IsNullOrEmpty(dbToken) || dbToken != rawToken))
                    {
                        context.Fail("Tu sesión ya no es válida: se inició sesión en otro dispositivo.");
                        return;
                    }

                    // Los roles del JWT deben coincidir con los privilegios del turno
                    // en este instante. Protege tokens antiguos y cambios de turno sin
                    // depender de que SignalR llegue a todos los dispositivos.
                    var user = await authRepository.GetByIdWithRelationsAsync(userId);
                    if (user == null || !user.Status)
                    {
                        context.Fail("La cuenta ya no está habilitada.");
                        return;
                    }

                    var scheduleRepository = context.HttpContext.RequestServices.GetRequiredService<
                        SaborExpress.Modules.EmployeeSchedules.Interfaces.IEmployeeScheduleRepository>();
                    var snapshot = await EffectiveRolePolicy.ResolveAsync(user, scheduleRepository);

                    var jwtRoles = context.Principal!.FindAll(ClaimTypes.Role)
                        .Select(claim => claim.Value)
                        .ToHashSet(StringComparer.Ordinal);
                    if (!jwtRoles.SetEquals(snapshot.Roles))
                    {
                        context.Fail("Los permisos de tu turno cambiaron. Inicia sesión de nuevo.");
                        return;
                    }

                    // Evita conservar un BranchId anterior si el empleado cambió de sede.
                    var tokenBranch = context.Principal.FindFirst("BranchId")?.Value;
                    var currentBranch = user.Employee?.BranchId?.ToString();
                    if (tokenBranch != currentBranch)
                    {
                        context.Fail("Tu sede cambió. Inicia sesión de nuevo.");
                        return;
                    }

                }
            };
        }
    }
}
