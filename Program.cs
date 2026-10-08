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

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // Los orígenes de dev tunnels / red local se permiten siempre,
        // sin importar el ambiente (evita que un ASPNETCORE_ENVIRONMENT
        // mal configurado tumbe la conexión de SignalR en pruebas).
        policy.SetIsOriginAllowed(origin =>
                    origin.EndsWith(".devtunnels.ms") ||
                    origin.StartsWith("http://localhost") ||
                    origin.StartsWith("https://localhost") ||
                    origin.StartsWith("http://192.168.") ||
                    allowedOrigins.Contains(origin))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
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
builder.Services.AddSignalR()
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

await app.MigrarYSembrarBaseDeDatosAsync();

app.UseSwaggerConfiguration();
app.UseStaticFiles();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications"); // ← NUEVO

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