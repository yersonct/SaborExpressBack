namespace SaborExpress.Modules.Tables.Interfaces
{
    public interface ITableAvailabilityService
    {
        Task<bool> TryReleaseAfterConfirmationAsync(int orderId, bool throwOnNotificationFailure = false);
        Task EnsureCanSetAvailableAsync(int tableId);
    }
}
