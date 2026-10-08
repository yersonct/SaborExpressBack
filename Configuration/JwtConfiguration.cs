using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SaborExpress.Modules.Auth.Interfaces;
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

                    var user = await authRepository.GetByIdWithRelationsAsync(userId);

                    if (user == null)
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

                    var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                    var singleSession = config.GetValue("Auth:SingleSession", true);

                    if (singleSession && (string.IsNullOrEmpty(user.Token) || user.Token != rawToken))
                    {
                        context.Fail("Tu sesión ya no es válida: se inició sesión en otro dispositivo.");
                    }
                }
            };
        }
    }
}