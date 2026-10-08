using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Shared.Helpers;

namespace SaborExpress.Modules.EmployeeSchedules.BackgroundServices
{
    public class RoleExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RoleExpirationBackgroundService> _logger;

        public RoleExpirationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<RoleExpirationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Espera hasta la próxima medianoche en hora Colombia ANTES de correr.
                // Antes se ejecutaba de inmediato cada vez que el backend se reiniciaba
                // (muy frecuente en desarrollo), y además comparaba contra DateTime.UtcNow,
                // que en las noches (Colombia UTC-5) ya es "mañana" en UTC. Esa combinación
                // hacía que el rol especial de HOY se marcara como vencido y se revocara
                // apenas arrancaba el backend, dejando solo el rol por defecto de la semana (Mesero).
                var now = ColombiaTime.Now;
                var nextRun = now.Date.AddDays(1); // próxima medianoche, hora Colombia
                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IEmployeeScheduleService>();
                    await service.RevokeExpiredRolesAsync();
                    _logger.LogInformation("Revocación diaria de roles vencidos ejecutada.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al revocar roles vencidos.");
                }
            }
        }
    }
}