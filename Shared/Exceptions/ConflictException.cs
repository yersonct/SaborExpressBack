namespace SaborExpress.Shared.Exceptions;

/// <summary>A domain state conflict that is safe to return as HTTP 409.</summary>
public sealed class ConflictException(string message) : InvalidOperationException(message);
