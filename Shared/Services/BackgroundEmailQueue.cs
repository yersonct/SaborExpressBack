using System.Threading.Channels;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Shared.Services
{
    /// <summary>
    /// Implementación con System.Threading.Channels: nativo de .NET, sin dependencias externas
    /// (nada de RabbitMQ/Redis). Suficiente para un solo servidor/instancia.
    /// </summary>
    public class BackgroundEmailQueue : IBackgroundEmailQueue
    {
        private readonly Channel<Func<CancellationToken, Task>> _queue;

        public BackgroundEmailQueue()
        {
            // Bounded: si se acumulan más de 500 correos sin procesar, espera en vez de explotar memoria.
            var options = new BoundedChannelOptions(500)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(options);
        }

        public async ValueTask QueueEmailAsync(Func<CancellationToken, Task> workItem)
        {
            ArgumentNullException.ThrowIfNull(workItem);
            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}