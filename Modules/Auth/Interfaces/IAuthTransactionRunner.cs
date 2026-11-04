namespace SaborExpress.Modules.Auth.Interfaces
{
    public interface IAuthTransactionRunner
    {
        Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    }
}
