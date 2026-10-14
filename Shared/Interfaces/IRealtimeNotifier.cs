namespace SaborExpress.Shared.Interfaces;

public interface IRealtimeNotifier
{
    Task OrdersChangedAsync();
    Task TablesChangedAsync();
    Task ProductsChangedAsync();
    Task PublicMenuChangedAsync();
    Task PublicBranchesChangedAsync();
    Task AdminDataChangedAsync(string module);
    Task ReviewsChangedAsync();
    Task PaymentsChangedAsync();

    Task DeliveriesChangedAsync();
    Task BranchOperationalSettingsChangedAsync(int branchId);
    Task DeliveryLocationUpdatedAsync(
        int customerUserId,
        int orderId,
        int deliveryId,
        decimal latitude,
        decimal longitude,
        DateTime updatedAt);

    // Manda a un usuario a iniciar sesión de nuevo si cambió su rol o sede.
    Task ForceLogoutAsync(int userId, string reason);

    // Cierra las sesiones anteriores cuando la cuenta inicia sesión en otro dispositivo.
    Task SessionReplacedAsync(int userId);
}
