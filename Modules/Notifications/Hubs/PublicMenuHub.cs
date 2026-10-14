using Microsoft.AspNetCore.SignalR;

namespace SaborExpress.Modules.Notifications.Hubs;

// Solo publica el aviso sin datos de menú; la carta vuelve a leer el menú
// mediante los endpoints públicos existentes.
public sealed class PublicMenuHub : Hub
{
}
