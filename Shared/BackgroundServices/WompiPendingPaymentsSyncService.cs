using SaborExpress.Modules.Payments.Interfaces;

namespace SaborExpress.Shared.BackgroundServices
{
    public class WompiPendingPaymentsSyncService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WompiPendingPaymentsSyncService> _logger;

        public WompiPendingPaymentsSyncService(
            IServiceScopeFactory scopeFactory,
            ILogger<WompiPendingPaymentsSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de sincronización Wompi iniciado");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                    await payments.SyncPendingWompiPaymentsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Fallo el ciclo de sincronización de pagos Wompi ({ErrorType}).",
                        ex.GetType().Name);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
