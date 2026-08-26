using System;

namespace SaborExpress.Modules.Auth.Exceptions
{
    public class TooManyResetAttemptsException : Exception
    {
        public int RetryAfterSeconds { get; }

        public TooManyResetAttemptsException(int retryAfterSeconds)
            : base($"Has alcanzado el limite de solicitudes. Intenta de nuevo en {FormatTime(retryAfterSeconds)}.")
        {
            RetryAfterSeconds = retryAfterSeconds;
        }

        private static string FormatTime(int seconds)
        {
            var minutes = seconds / 60;
            return minutes >= 1 ? $"{minutes} minuto(s)" : $"{seconds} segundos";
        }
    }
}
