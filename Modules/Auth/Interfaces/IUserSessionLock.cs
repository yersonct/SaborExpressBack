namespace SaborExpress.Modules.Auth.Interfaces
{
    /// <summary>
    /// Serializes login, refresh and password recovery for one user in this API process.
    /// Database transactions provide the corresponding protection across API instances.
    /// </summary>
    public interface IUserSessionLock
    {
        ValueTask<IAsyncDisposable> AcquireAsync(int userId, CancellationToken cancellationToken = default);
    }
}
