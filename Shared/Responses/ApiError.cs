namespace SaborExpress.Shared.Responses;

public sealed record ApiError(string Code, string Message, string TraceId,
    int? RetryAfterSeconds = null)
{
    // Retain the legacy field while existing released clients adopt Message.
    public string Error => Message;
}
