using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SaborExpress.Configuration;
using System.Text;

namespace SaborExpress.Configuration
{
    public static class JwtConfiguration
    {
        public static IServiceCollection AddJwtConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Registrar JwtSettings para usar IOptions<JwtSettings>
            services.Configure<JwtSettings>(
                configuration.GetSection("JwtSettings"));

            var secretKey = configuration["JwtSettings:SecretKey"];
            var issuer = configuration["JwtSettings:Issuer"];
            var audience = configuration["JwtSettings:Audience"];
            var expiration = configuration.GetValue<int>(
                "JwtSettings:ExpirationInMinutes"
            );

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new Exception(
                    "JwtSettings:SecretKey no está configurado en User Secrets o appsettings"
                );
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = issuer,
                        ValidAudience = audience,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(secretKey)
                        ),

                        ClockSkew = TimeSpan.Zero
                    };
                });

            return services;
        }
    }
}