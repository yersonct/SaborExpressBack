using System;
using System.Collections.Generic;
using System.Linq;

namespace SaborExpress.Modules.Auth.Helpers
{
    public static class PasswordResetRateLimiter
    {
        public const int MaxAttempts = 3;
        public const int WindowMinutes = 15;

        public static int? GetRetryAfterSecondsIfLocked(List<DateTime> recentAttemptTimestamps)
        {
            if (recentAttemptTimestamps.Count < MaxAttempts)
                return null;

            var oldestAttempt = recentAttemptTimestamps
                .OrderBy(t => t)
                .First();

            var lockoutEnd = oldestAttempt.AddMinutes(WindowMinutes);
            var remaining = (int)Math.Max(0, (lockoutEnd - DateTime.UtcNow).TotalSeconds);

            return remaining > 0 ? remaining : null;
        }
    }
}
