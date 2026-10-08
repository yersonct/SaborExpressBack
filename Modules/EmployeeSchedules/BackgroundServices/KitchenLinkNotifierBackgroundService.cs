using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Shared.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SaborExpress.Modules.EmployeeSchedules.BackgroundServices
{
    // Cada X minutos revisa que cocineros tienen turno ACTIVO ahora mismo (ya empezo,
    // no ha terminado) y les manda por correo el link del monitor de cocina de SU sede,
    // una sola vez por turno (KitchenLinkSent evita reenviarlo mientras dure el turno).
    public class KitchenLinkNotifierBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KitchenLinkNotifierBackgroundService> _logger;
        private readonly IConfiguration _config;

        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        public KitchenLinkNotifierBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<KitchenLinkNotifierBackgroundService> logger,
            IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _config = config;
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
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var baseUrl = _config["AppSettings:PublicBaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("AppSettings:PublicBaseUrl no esta configurado, no se pueden mandar links de cocina.");
                return;
            }

            var activeShifts = await scheduleRepository.GetActiveCookShiftsToNotifyAsync(DateTime.Now);

            foreach (var schedule in activeShifts)
            {
                var email = schedule.Employee?.User?.Email;
                var accessToken = Guid.NewGuid().ToString("N");

                if (string.IsNullOrEmpty(email))
                {
                    // Sin correo, no hay a dónde mandarlo, pero igual guardamos el
                    // token por si luego se resuelve el correo del empleado.
                    await scheduleRepository.MarkKitchenLinkSentAsync(schedule.Id, accessToken);
                    continue;
                }

                var link = $"{baseUrl}/kitchen-board/token/{accessToken}";
                var employeeName = $"{schedule.Employee!.Name} {schedule.Employee.LastName}".Trim();

                try
                {
                    await notificationService.SendEmailAsync(
                        email,
                        "Monitor de cocina - Tu turno de hoy",
                        $"Hola {employeeName}, este es tu link personal del monitor de cocina para este turno " +
                        $"(deja de funcionar cuando termine tu turno): <a href=\"{link}\">{link}</a>");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo enviar el link de cocina a {Email}", email);
                }

                await scheduleRepository.MarkKitchenLinkSentAsync(schedule.Id, accessToken);
            }

            if (activeShifts.Count > 0)
                _logger.LogInformation("Se enviaron {Count} links de monitor de cocina.", activeShifts.Count);
        }
    }
}