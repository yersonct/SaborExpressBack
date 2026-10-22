using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SaborExpress.Configuration;
using SaborExpress.Data;
using SaborExpress.Data.SeedData;
using SaborExpress.Shared.Filters;
using SaborExpress.Modules.Notifications.Hubs; 
using SaborExpress.Modules.EmployeeSchedules.BackgroundServices;
using SaborExpress.Shared.BackgroundServices;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.Branches.Services;
using SaborExpress.Shared.Authorization;
using SaborExpress.Shared.Localization;
using SaborExpress.Shared.Middleware;
using SaborExpress.Shared.Security;

// Carga el archivo .env ANTES de crear el builder
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var configuredOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
var isDevelopment = builder.Environment.IsDevelopment();
var allowedOrigins = CorsOriginPolicy.FilterConfiguredOrigins(configuredOrigins, isDevelopment);

if (!isDevelopment && allowedOrigins.Count == 0)
    throw new InvalidOperationException(
        "Configure al menos un origen productivo exacto en AllowedOrigins antes de iniciar el backend.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                CorsOriginPolicy.IsAllowedOrigin(origin, allowedOrigins, isDevelopment))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Server-Timing")
              .AllowCredentials();
    });
});

builder.Services.AddSingleton<IApiMessageLocalizer, ApiMessageLocalizer>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ApiExceptionFilter>();
builder.Services.AddScoped<ApiMessageLocalizationFilter>();

builder.Services.AddControllers(options =>
{
    options.Filters.AddService<ApiExceptionFilter>();
    options.Filters.AddService<ApiMessageLocalizationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);

// NUEVO: validación de turno activo para roles operativos
builder.Services.AddScoped<IAuthorizationHandler, ActiveShiftHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveShift", policy =>
        policy.Requirements.Add(new ActiveShiftRequirement()));
});

builder.Services.AddDependencies();
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
})
    .AddJsonProtocol(options =>
    {
        // Mismo formato que la API REST: enums como texto ("PaymentConfirmed"), no como número
        options.PayloadSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    }); 

builder.Services.AddHostedService<ShiftReminderBackgroundService>();
builder.Services.AddHostedService<KitchenLinkNotifierBackgroundService>();
builder.Services.AddHostedService<RoleExpirationBackgroundService>();
builder.Services.AddHostedService<WompiPendingPaymentsSyncService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

await app.MigrarYSembrarBaseDeDatosAsync();

if (app.Environment.IsDevelopment())
    app.UseSwaggerConfiguration();
app.UseRouting();
app.UseMiddleware<ApiLatencyMetricsMiddleware>();
app.UseStaticFiles();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseMiddleware<ClosedBranchGuardMiddleware>();
app.UseMiddleware<WebRequestRealtimeMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications"); // ← NUEVO
app.MapHub<PublicMenuHub>("/hubs/public-menu");

app.Run();

static class ProgramExtensions
{
    public static async Task MigrarYSembrarBaseDeDatosAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await context.Database.MigrateAsync();
        await PermissionSeeder.SeedAsync(context);
        await ClienteRoleSeeder.SeedAsync(context);  
        await InitialManagerSeeder.SeedAsync(context, configuration);
    }
}
