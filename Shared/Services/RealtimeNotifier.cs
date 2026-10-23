using Microsoft.AspNetCore.SignalR;
using SaborExpress.Modules.Notifications.Hubs;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Shared.Services;

public class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IHubContext<PublicMenuHub> _publicMenuHub;
    private readonly ILogger<RealtimeNotifier> _logger;

    public RealtimeNotifier(
        IHubContext<NotificationHub> hub,
        IHubContext<PublicMenuHub> publicMenuHub,
        ILogger<RealtimeNotifier> logger)
    {
        _hub = hub;
        _publicMenuHub = publicMenuHub;
        _logger = logger;
    }

    public async Task OrdersChangedAsync()
    {
        await Task.WhenAll(
            SendSafeAsync("OrdersChanged"),
            SendPublicKitchenChangedSafe());
    }
    public Task TablesChangedAsync() => SendSafeAsync("TablesChanged");
    public Task ProductsChangedAsync() => SendSafeAsync("ProductsChanged");
    public Task PaymentsChangedAsync() => SendSafeAsync("PaymentsChanged");
    public Task PublicMenuChangedAsync() => SendPublicMenuChangedSafe();
    public async Task PublicBranchesChangedAsync()
    {
        await Task.WhenAll(
            SendPublicBranchesChangedSafe(),
            SendSafeAsync("PublicBranchesChanged"));
    }
    public Task AdminDataChangedAsync(string module) => SendAdminDataChangedSafe(module);
    public Task ReviewsChangedAsync() => SendSafeAsync("ReviewsChanged");
    public Task DeliveriesChangedAsync() => SendSafeAsync("DeliveriesChanged");
    public Task BranchOperationalSettingsChangedAsync(int branchId) =>
        SendBranchOperationalSettingsChangedSafe(branchId);

    public async Task DeliveryLocationUpdatedAsync(
        int customerUserId,
        int orderId,
        int deliveryId,
        decimal latitude,
        decimal longitude,
        DateTime updatedAt)
    {
        try
        {
            await _hub.Clients.Group(NotificationHub.GroupName(customerUserId))
                .SendAsync("DeliveryLocationUpdated", new
                {
                    orderId,
                    deliveryId,
                    latitude,
                    longitude,
                    updatedAt
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo enviar la ubicación en vivo ({ErrorType}).", ex.GetType().Name);
        }
    }

    private async Task SendBranchOperationalSettingsChangedSafe(int branchId)
    {
        try
        {
            _logger.LogDebug("[Realtime] Emitiendo BranchOperationalSettingsChanged para la sede {BranchId}", branchId);
            await _hub.Clients.All.SendAsync("BranchOperationalSettingsChanged", branchId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo emitir BranchOperationalSettingsChanged ({ErrorType}).", ex.GetType().Name);
        }
    }

    public async Task ForceLogoutAsync(int userId, string reason)
    {
        try
        {
            await _hub.Clients.Group(NotificationHub.GroupName(userId)).SendAsync("ForceLogout", reason);
        }
        catch (Exception ex) { _logger.LogWarning("No se pudo emitir ForceLogout ({ErrorType}).", ex.GetType().Name); }
    }

    public async Task SessionReplacedAsync(int userId)
    {
        try
        {
            await _hub.Clients.Group(NotificationHub.GroupName(userId)).SendAsync("SessionReplaced");
        }
        catch (Exception ex) { _logger.LogWarning("No se pudo cerrar la sesión anterior ({ErrorType}).", ex.GetType().Name); }
    }

    // Un fallo del socket nunca debe romper la operación de negocio.
    private async Task SendSafeAsync(string method)
    {
        try
        {
            _logger.LogDebug("[Realtime] Emitiendo {Method}", method);
            await _hub.Clients.All.SendAsync(method);
        }
        catch (Exception ex) { _logger.LogWarning("No se pudo emitir una actualización en tiempo real ({ErrorType}).", ex.GetType().Name); }
    }

    private async Task SendPublicMenuChangedSafe()
    {
        try
        {
            await _publicMenuHub.Clients.All.SendAsync("MenuChanged");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo avisar que cambió el menú público ({ErrorType}).", ex.GetType().Name);
        }
    }

    private async Task SendPublicBranchesChangedSafe()
    {
        try
        {
            await _publicMenuHub.Clients.All.SendAsync("BranchesChanged");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo avisar que cambiaron las sedes públicas ({ErrorType}).", ex.GetType().Name);
        }
    }

    private async Task SendPublicKitchenChangedSafe()
    {
        try
        {
            await _publicMenuHub.Clients.All.SendAsync("KitchenBoardChanged");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo avisar que cambiaron los pedidos de cocina ({ErrorType}).", ex.GetType().Name);
        }
    }

    private async Task SendAdminDataChangedSafe(string module)
    {
        try
        {
            await _hub.Clients
                .Group(NotificationHub.AdminDataUpdatesGroupName)
                .SendAsync("AdminDataChanged", new
                {
                    module,
                    method = "BACKGROUND",
                    changedAt = DateTimeOffset.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo avisar que cambiaron datos administrativos ({ErrorType}).", ex.GetType().Name);
        }
    }
}
