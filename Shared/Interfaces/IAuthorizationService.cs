namespace SaborExpress.Shared.Interfaces
{
    public interface IAuthorizationService
    {
        Task<bool> CanPerformActionAsync(int employeeId, string permissionName);
    }
}