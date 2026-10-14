namespace SaborExpress.Modules.Tables.Interfaces
{
    public interface ITableAvailabilityService
    {
        Task<bool> TryReleaseAfterConfirmationAsync(int orderId);
        Task EnsureCanSetAvailableAsync(int tableId);
    }
}
