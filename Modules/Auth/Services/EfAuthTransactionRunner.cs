using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        private const int MaxAttempts = 3;

        public async Task<T> ExecuteSerializableAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await using var transaction = await db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);

                    var result = await operation();
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch (Exception exception)
                {
                    // A failed PostgreSQL transaction leaves tracked values stale. Clear them
                    // after rollback/disposal before a retry so the callback re-reads committed data.
                    db.ChangeTracker.Clear();

                    if (attempt >= MaxAttempts || !HasRetryablePostgresFailure(exception))
                        throw;

                    var backoffMilliseconds = (25 * (1 << (attempt - 1))) + Random.Shared.Next(0, 26);
                    await Task.Delay(backoffMilliseconds, cancellationToken);
                }
            }
        }

        private static bool HasRetryablePostgresFailure(Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException!)
            {
                if (current is PostgresException postgresException &&
                    (postgresException.SqlState == PostgresErrorCodes.SerializationFailure ||
                     postgresException.SqlState == PostgresErrorCodes.DeadlockDetected))
                {
                    return true;
                }

                if (current is AggregateException aggregateException &&
                    aggregateException.InnerExceptions.Any(HasRetryablePostgresFailure))
                {
                    return true;
                }

                if (current.InnerException is null)
                    break;
            }

            return false;
        }
    }
}
