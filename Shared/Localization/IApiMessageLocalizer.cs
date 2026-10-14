using System.Text.Json.Nodes;

namespace SaborExpress.Shared.Localization;

public interface IApiMessageLocalizer
{
    string ResolveLanguage(string? acceptLanguage);
    string Localize(string? message, string? language);
    string LocalizeError(string? message, string? language);
    string GenericError(string? language);
    JsonNode? LocalizePayload(object? payload, string? language, bool isErrorResponse);
}
