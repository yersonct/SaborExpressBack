using Microsoft.EntityFrameworkCore;
using SaborExpress.Configuration;
using SaborExpress.Data;
using SaborExpress.Data.SeedData;
using SaborExpress.Shared.Filters;
using SaborExpress.Modules.Notifications.Hubs; // ← NUEVO
using SaborExpress.Modules.EmployeeSchedules.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});
Console.WriteLine($"DB PROVIDER >>> {builder.Configuration.GetValue<string>("DatabaseProvider")}");
Console.WriteLine($"CONNECTION STRING >>> {builder.Configuration.GetConnectionString("PostgresConnection")}");

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.AddDependencies();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddSignalR(); 
builder.Services.AddHostedService<ShiftReminderBackgroundService>();

var app = builder.Build();

await app.MigrarYSembrarBaseDeDatosAsync();

if (app.Environment.IsDevelopment())
    app.UseSwaggerConfiguration();

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