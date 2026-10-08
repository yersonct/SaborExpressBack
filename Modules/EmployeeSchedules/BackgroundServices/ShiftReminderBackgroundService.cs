using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Modules.EmployeeSchedules.Models;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Notifications.Enum;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Shared.Helpers;

namespace SaborExpress.Modules.EmployeeSchedules.BackgroundServices
{
    // "El reloj": cada X minutos hace 2 cosas:
    // 1. Avisa al Administrador si el turno de HOY de un empleado termina pronto
    //    y todavia no tiene turno programado para MANANA.
    // 2. Si un turno de HOY ya termino y sigue sin turno de manana (nadie hizo nada),
    //    lo copia automaticamente para el dia siguiente.
    public class ShiftReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ShiftReminderBackgroundService> _logger;

        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
        private const int MinutesBeforeEndToNotify = 15;

        public ShiftReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ShiftReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Espera el intervalo completo ANTES de la primera corrida. Antes,
            // este chequeo se disparaba de inmediato cada vez que el backend
            // se reiniciaba (muy frecuente en desarrollo), aumentando el riesgo
            // de duplicar/clonar turnos de hoy con el rol viejo si la fecha
            // calculada quedaba desfasada en ese instante.
            await Task.Delay(CheckInterval, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndNotifyAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al revisar turnos.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CheckAndNotifyAsync()
        {
            using var scope = _scopeFactory.CreateScope();

            var scheduleRepository = scope.ServiceProvider.GetRequiredService<IEmployeeScheduleRepository>();
            var employeeRepository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IUserNotificationService>();

            await SendReminderToAdminsAsync(scheduleRepository, employeeRepository, notificationService);
            await AutoRenewShiftsAsync(scheduleRepository);
        }

        private async Task SendReminderToAdminsAsync(
            IEmployeeScheduleRepository scheduleRepository,
            IEmployeeRepository employeeRepository,
            IUserNotificationService notificationService)
        {
        var now = ColombiaTime.Now;
        var windowEnd = now.AddMinutes(MinutesBeforeEndToNotify);
        var tomorrow = DateOnly.FromDateTime(now.AddDays(1));

            var shiftsEndingSoon = await scheduleRepository.GetShiftsEndingSoonAsync(now, windowEnd);

            foreach (var schedule in shiftsEndingSoon)
            {
                var tomorrowSchedules = await scheduleRepository.GetByEmployeeAndDateAsync(schedule.EmployeeId, tomorrow);
                if (tomorrowSchedules.Count > 0)
                {
                    // Ya tiene turno de manana definido, no hace falta avisar
                    await scheduleRepository.MarkReminderSentAsync(schedule.Id);
                    continue;
                }

                var adminUserIds = await employeeRepository.GetAdminUserIdsByBranchAsync(schedule.BranchId);
                if (adminUserIds.Count == 0)
                {
                    await scheduleRepository.MarkReminderSentAsync(schedule.Id);
                    continue;
                }

                var employeeName = $"{schedule.Employee.Name} {schedule.Employee.LastName}".Trim();

                await notificationService.CreateBulkAsync(
                    userIds: adminUserIds,
                    title: "Turno sin programar para mañana",
                    message: $"El turno de {employeeName} ({schedule.Role?.Name}) termina hoy y no tiene turno asignado para mañana. Si no defines uno nuevo, se repetira el mismo turno automaticamente.",
                    type: NotificationType.ShiftEndingSoon,
                    relatedEntityType: "EmployeeSchedule",
                    relatedEntityId: schedule.Id
                );

                await scheduleRepository.MarkReminderSentAsync(schedule.Id);
            }

            if (shiftsEndingSoon.Count > 0)
                _logger.LogInformation("Se revisaron {Count} turnos por terminar.", shiftsEndingSoon.Count);
        }

        private async Task AutoRenewShiftsAsync(IEmployeeScheduleRepository scheduleRepository)
        {
            var now = ColombiaTime.Now;
            var tomorrow = DateOnly.FromDateTime(now.AddDays(1));
            var endedShifts = await scheduleRepository.GetShiftsEndedTodayAsync(now);

            foreach (var schedule in endedShifts)
            {
                var tomorrowSchedules = await scheduleRepository.GetByEmployeeAndDateAsync(schedule.EmployeeId, tomorrow);
                if (tomorrowSchedules.Count > 0)
                    continue; // el Administrador ya definio algo, no tocar

                var renewed = new EmployeeSchedule
                {
                    EmployeeId = schedule.EmployeeId,
                    BranchId = schedule.BranchId,
                    RoleId = schedule.RoleId,
                    ShiftDate = tomorrow,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    Notes = "Renovado automaticamente (el Administrador no definio un turno distinto)"
                };

                await scheduleRepository.AddAsync(renewed);
                _logger.LogInformation("Turno renovado automaticamente para EmployeeId {EmployeeId}.", schedule.EmployeeId);
            }
        }
    }
}