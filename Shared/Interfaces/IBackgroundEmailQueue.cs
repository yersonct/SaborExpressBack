namespace SaborExpress.Shared.Interfaces
{
    /// <summary>
    /// Cola en memoria para trabajos "dispara y olvida" (envío de correo, notificaciones, etc.).
    /// El objetivo es que el controller/servicio que encola NO espere a que el trabajo termine,
    /// para que el usuario reciba su respuesta HTTP de inmediato.
    /// </summary>
    public interface IBackgroundEmailQueue
    {
        /// <summary>
        /// Encola un trabajo para que se ejecute en segundo plano, en orden (FIFO).
        /// </summary>
        ValueTask QueueEmailAsync(Func<CancellationToken, Task> workItem);

        /// <summary>
        /// Usado internamente por el procesador en background para sacar el siguiente trabajo.
        /// </summary>
        ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}