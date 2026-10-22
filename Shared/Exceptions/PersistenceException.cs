namespace SaborExpress.Shared.Exceptions;

public sealed class PersistenceException : Exception
{
    public PersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
