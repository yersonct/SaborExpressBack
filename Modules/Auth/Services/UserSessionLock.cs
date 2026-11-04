using System.Collections.Concurrent;
using SaborExpress.Modules.Auth.Interfaces;

namespace SaborExpress.Modules.Auth.Services
{
    public sealed class UserSessionLock : IUserSessionLock
    {
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

        public async ValueTask<IAsyncDisposable> AcquireAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var semaphore = _locks.GetOrAdd(userId, static _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken);
            return new Lease(semaphore);
        }

        private sealed class Lease(SemaphoreSlim semaphore) : IAsyncDisposable
        {
            private SemaphoreSlim? _semaphore = semaphore;

            public ValueTask DisposeAsync()
            {
                Interlocked.Exchange(ref _semaphore, null)?.Release();
                return ValueTask.CompletedTask;
            }
        }
    }
}
