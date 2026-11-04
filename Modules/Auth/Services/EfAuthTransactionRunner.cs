using System.Data;
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.Interfaces;

namespace SaborExpress.Modules.Auth.Services
{
    /// <summary>
    /// Runs auth mutations in a serializable transaction on the request-scoped
    /// AppDbContext shared by the auth, reset-code and refresh-token repositories.
    /// An exception before CommitAsync causes EF Core to roll back on disposal.
    /// </summary>
    public sealed class EfAuthTransactionRunner(AppDbContext db) : IAuthTransactionRunner
    {
        public async Task<T> ExecuteSerializableAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
    }
}
