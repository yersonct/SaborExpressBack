using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Shared.BackgroundServices
{
    /// <summary>
    /// Corre en segundo plano toda la vida de la app. Va sacando trabajos de la cola
    /// uno por uno y los ejecuta. Aquí es donde realmente se llama al SMTP.
    /// </summary>
    public class EmailQueueProcessor : BackgroundService
    {
        private readonly IBackgroundEmailQueue _queue;
        private readonly ILogger<EmailQueueProcessor> _logger;

        public EmailQueueProcessor(IBackgroundEmailQueue queue, ILogger<EmailQueueProcessor> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _queue.DequeueAsync(stoppingToken);

                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    // Un correo que falla NO debe tumbar el procesador ni afectar al usuario
                    // que ya recibió su 200 OK hace rato.
                    _logger.LogError(ex, "Error al procesar un trabajo en background (envio de correo).");
                }
            }
        }
    }
}