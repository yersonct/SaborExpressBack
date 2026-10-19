using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Shared.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SaborExpress.Modules.EmployeeSchedules.BackgroundServices
{
    // Cada minuto prepara el token del monitor para turnos activos de cocina.
    // Ya NO se envía ningún enlace automático por correo al personal.
    public class KitchenLinkNotifierBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KitchenLinkNotifierBackgroundService> _logger;

        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        public KitchenLinkNotifierBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<KitchenLinkNotifierBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al revisar cocineros con turno activo.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CheckAndSendAsync()
        {
            using var scope = _scopeFactory.CreateScope();

            var scheduleRepository = scope.ServiceProvider.GetRequiredService<IEmployeeScheduleRepository>();
            var activeShifts = await scheduleRepository.GetActiveCookShiftsToNotifyAsync(DateTime.Now);

            foreach (var schedule in activeShifts)
            {
                // El token de acceso al monitor se conserva para el funcionamiento
                // interno, pero NUNCA se envía el enlace por correo al empleado.
                var accessToken = Guid.NewGuid().ToString("N");
                await scheduleRepository.MarkKitchenLinkSentAsync(schedule.Id, accessToken);
            }

            if (activeShifts.Count > 0)
                _logger.LogDebug("Se prepararon {Count} accesos de monitor de cocina sin enviar correos.", activeShifts.Count);
        }
    }
}