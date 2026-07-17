using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Repositories;
using SaborExpress.Modules.Auth.Services;
using SaborExpress.Modules.Auth.Validators;

namespace SaborExpress.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencies(this IServiceCollection services)
        {
            services.AddScoped<RegisterValidator>();

            services.AddScoped<LoginValidator>();

            services.AddScoped<JwtTokenGenerator>();

            services.AddScoped<IAuthRepository, AuthRepository>();

            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}