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
using SaborExpress.Shared.Observability;

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
builder.Services.AddSingleton<ApiMetricsRegistry>();
builder.Services.AddSingleton<ApiRouteCatalog>();
builder.Services.AddScoped<ApiExceptionFilter>();
builder.Services.AddScoped<ApiMessageLocalizationFilter>();

builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new ApiVersionConvention());
    options.Filters.Add<ApiPaginationFilter>();
    options.Filters.AddService<ApiExceptionFilter>();
    options.Filters.AddService<ApiMessageLocalizationFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ", context.ModelState.Values.SelectMany(value => value.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Datos de solicitud inválidos." : error.ErrorMessage));
        var localizer = context.HttpContext.RequestServices.GetRequiredService<IApiMessageLocalizer>();
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new SaborExpress.Shared.Responses.ApiError(
            "VALIDATION_ERROR", localizer.LocalizeError(message, context.HttpContext.Request.Headers.AcceptLanguage.ToString()),
            context.HttpContext.TraceIdentifier));
    };
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
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("ActiveShift", policy =>
        policy.Requirements.Add(new ActiveShiftRequirement()));
});

builder.Services.AddDependencies();
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>()
    .AddHttpMessageHandler(provider => new ProviderMetricsDelegatingHandler(
        provider.GetRequiredService<ApiMetricsRegistry>(), "geocoding"));
builder.Services.AddHttpClient("email")
    .AddHttpMessageHandler(provider => new ProviderMetricsDelegatingHandler(
        provider.GetRequiredService<ApiMetricsRegistry>(), "email"));
builder.Services.AddHttpClient("push")
    .AddHttpMessageHandler(provider => new ProviderMetricsDelegatingHandler(
        provider.GetRequiredService<ApiMetricsRegistry>(), "push"));
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

var exportArgument = args.FirstOrDefault(arg => arg.StartsWith("--export-openapi=", StringComparison.Ordinal));
if (exportArgument is not null)
{
    var output = Path.GetFullPath(exportArgument["--export-openapi=".Length..]);
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    var document = app.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>().GetSwagger("v1");
    using var stream = File.CreateText(output);
    document.SerializeAsV3(new Microsoft.OpenApi.Writers.OpenApiJsonWriter(stream));
    Console.WriteLine("OpenAPI exportado correctamente.");
    return;
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health/ready", async (AppDbContext context, CancellationToken cancellationToken) =>
{
    try
    {
        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            return Results.Json(
                new { status = "not_ready", database = "unavailable" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var hasPendingMigrations = (await context.Database
            .GetPendingMigrationsAsync(cancellationToken))
            .Any();

        if (hasPendingMigrations)
        {
            return Results.Json(
                new { status = "not_ready", database = "connected", migrations = "pending" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new { status = "ready", database = "connected", migrations = "applied" });
    }
    catch
    {
        // Readiness is intentionally generic: connection details must never reach the response.
        return Results.Json(
            new { status = "not_ready", database = "unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    if (!app.Environment.IsDevelopment() &&
        !app.Configuration.GetValue("Database:BackupConfirmed", false))
        throw new InvalidOperationException("Confirma Database:BackupConfirmed antes de migrar fuera de desarrollo.");
    await app.MigrarYSembrarBaseDeDatosAsync();
}

app.UseSwagger();
if (app.Environment.IsDevelopment())
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "SaborExpress API v1"));
app.UseStatusCodePages(async status =>
{
    var response = status.HttpContext.Response;
    var localizer = status.HttpContext.RequestServices.GetRequiredService<IApiMessageLocalizer>();
    var code = response.StatusCode switch
    {
        401 => "UNAUTHORIZED", 403 => "FORBIDDEN", 404 => "NOT_FOUND",
        429 => "RATE_LIMITED", _ => "HTTP_ERROR"
    };
    var message = response.StatusCode switch
    {
        401 => "No se pudo identificar al usuario autenticado.",
        403 => "No tienes permiso para realizar esta acción.",
        404 => "El recurso solicitado no existe.",
        _ => "No se pudo completar la solicitud."
    };
    await response.WriteAsJsonAsync(new SaborExpress.Shared.Responses.ApiError(code,
        localizer.LocalizeError(message, status.HttpContext.Request.Headers.AcceptLanguage.ToString()),
        status.HttpContext.TraceIdentifier));
});
app.UseRouting();
app.UseMiddleware<ApiLatencyMetricsMiddleware>();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context => context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseMiddleware<ClosedBranchGuardMiddleware>();
app.UseMiddleware<WebRequestRealtimeMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications"); // ← NUEVO
app.MapHub<PublicMenuHub>("/hubs/public-menu").AllowAnonymous();

app.MapGet("/metrics", (ApiMetricsRegistry metrics) =>
        Results.Text(metrics.RenderPrometheus(), "text/plain; version=0.0.4; charset=utf-8"))
    .RequireAuthorization(policy => policy.RequireRole(SaborExpress.Shared.Constants.RoleNames.Gerente));

app.Run();

static class ProgramExtensions
{
    public static async Task MigrarYSembrarBaseDeDatosAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await context.Database.MigrateAsync();
        if (configuration.GetValue("Database:SeedOnStartup", false))
        {
            await PermissionSeeder.SeedAsync(context);
            await ClienteRoleSeeder.SeedAsync(context);
            await InitialManagerSeeder.SeedAsync(context, configuration);
        }
    }
}

public partial class Program { }
