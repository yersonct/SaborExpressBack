using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Notifications.Models;

namespace SaborExpress.Modules.Notifications.Services
{
    public sealed record ExpoPushMessage(
        int UserId,
        string Title,
        string Body,
        int NotificationId,
        string Type,
        string? RelatedEntityType,
        int? RelatedEntityId);

    public interface IExpoPushNotificationQueue
    {
        bool Enqueue(ExpoPushMessage message);
    }

    // Envía push fuera de la petición del pedido para que una demora del servicio
    // de Expo no aumente el tiempo de respuesta de la API.
    public sealed class ExpoPushNotificationQueue : BackgroundService, IExpoPushNotificationQueue
    {
        private readonly Channel<ExpoPushMessage> _messages =
            Channel.CreateUnbounded<ExpoPushMessage>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExpoPushNotificationQueue> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ExpoPushNotificationQueue(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ExpoPushNotificationQueue> logger,
            IServiceScopeFactory scopeFactory)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public bool Enqueue(ExpoPushMessage message) => _messages.Writer.TryWrite(message);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var message in _messages.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await SendToUserDevicesAsync(message, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning("No se pudo procesar una notificación push ({ErrorType}).", exception.GetType().Name);
                }
            }
        }

        private async Task SendToUserDevicesAsync(ExpoPushMessage message, CancellationToken stoppingToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var devices = await repository.GetPushDevicesByUserIdAsync(message.UserId);
            foreach (var device in devices)
            {
                // Mesero: pedidos listos; cajero: pedidos nuevos y pagos;
                // repartidor: domicilios asignados y cambios del pedido.
                if (device.NotificationProfile == "waiter" &&
                    message.Type is not ("OrderReady" or "TableAvailable"))
                    continue;
                if (device.NotificationProfile == "cashier"
                    && message.Type == "OrderCreated"
                    && message.Title is not ("Pedido nuevo" or "New order" or "Novo pedido" or "Nouvelle commande"))
                    continue;
                if (device.NotificationProfile == "cashier"
                    && message.Type != "OrderCreated"
                    && message.Type != "PaymentConfirmed")
                    continue;
                if (device.NotificationProfile == "delivery" &&
                    message.Type is not ("DeliveryAssigned" or "OrderReady" or "OrderStatusChanged"))
                    continue;
                if (device.NotificationProfile != "waiter" &&
                    device.NotificationProfile != "cashier" &&
                    device.NotificationProfile != "delivery")
                    continue;

                var tokenStillRegistered = await SendWithRetryAsync(message, device, stoppingToken);
                if (!tokenStillRegistered)
                    await repository.RemovePushDeviceAsync(message.UserId, device.ExpoPushToken);
            }
        }

        private async Task<bool> SendWithRetryAsync(
            ExpoPushMessage message,
            PushDevice device,
            CancellationToken stoppingToken)
        {
            var payload = new
            {
                to = device.ExpoPushToken,
                title = message.Title,
                body = message.Body,
                sound = device.SoundEnabled ? "default" : null,
                channelId = device.NotificationProfile switch
                {
                    "cashier" => $"cashier-alerts-v1-{(device.SoundEnabled ? "sound" : "silent")}-{(device.VibrationEnabled ? "vibrate" : "quiet")}",
                    "delivery" => $"delivery-orders-v2-{(device.SoundEnabled ? "sound" : "silent")}-{(device.VibrationEnabled ? "vibrate" : "quiet")}",
                    _ => $"waiter-orders-v3-{(device.SoundEnabled ? "sound" : "silent")}-{(device.VibrationEnabled ? "vibrate" : "quiet")}"
                },
                priority = "high",
                // No dejar pedidos antiguos esperando en FCM/APNs durante dÃ­as.
                ttl = 300,
                data = new
                {
                    notificationId = message.NotificationId,
                    type = message.Type,
                    relatedEntityType = message.RelatedEntityType,
                    relatedEntityId = message.RelatedEntityId
                }
            };

            var client = _httpClientFactory.CreateClient("push");
            var accessToken = _configuration["Expo:PushAccessToken"]
                ?? _configuration["EXPO_PUSH_ACCESS_TOKEN"];

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Post,
                        "https://exp.host/--/api/v2/push/send")
                    {
                        Content = JsonContent.Create(payload)
                    };
                    if (!string.IsNullOrWhiteSpace(accessToken))
                        request.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    using var response = await client.SendAsync(request, stoppingToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(stoppingToken);
                        var ticketError = ReadTicketError(body);
                        if (ticketError == "DeviceNotRegistered") return false;
                        if (ticketError != null)
                            _logger.LogWarning("Expo reportó un error de ticket push: {Error}.", ticketError);
                        return true;
                    }

                    if ((int)response.StatusCode < 500 && (int)response.StatusCode != 429)
                    {
                        _logger.LogWarning("Expo rechazó una notificación push con HTTP {StatusCode}.", response.StatusCode);
                        return true;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return true;
                }
                catch (Exception exception)
                {
                    if (attempt == 2)
                        _logger.LogWarning("No se pudo enviar una notificación push a Expo ({ErrorType}).", exception.GetType().Name);
                }

                if (attempt < 2)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(500 * (attempt + 1)), stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return true;
                    }
                }
            }
            return true;
        }

        private static string? ReadTicketError(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                if (!document.RootElement.TryGetProperty("data", out var data)) return null;
                var ticket = data.ValueKind == JsonValueKind.Array
                    ? data.EnumerateArray().FirstOrDefault()
                    : data;
                if (ticket.ValueKind != JsonValueKind.Object
                    || !ticket.TryGetProperty("status", out var status)
                    || status.ValueKind != JsonValueKind.String
                    || status.GetString() != "error"
                    || !ticket.TryGetProperty("details", out var details)
                    || details.ValueKind != JsonValueKind.Object
                    || !details.TryGetProperty("error", out var error))
                    return null;
                return error.GetString();
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
