namespace SaborExpress.Modules.Auth.Exceptions
{
    public class TooManyLoginAttemptsException : Exception
    {
        public int RetryAfterSeconds { get; }

        public TooManyLoginAttemptsException(int retryAfterSeconds)
            : base("Demasiados intentos fallidos. Intenta de nuevo mas tarde.")
        {
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}
