using System.Collections;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaborExpress.Shared.Localization;
using SaborExpress.Shared.Responses;

namespace SaborExpress.Shared.Filters;

/// <summary>Normalizes controller-produced HTTP errors to the public API error shape.</summary>
public sealed class ApiErrorResultFilter(IApiMessageLocalizer localizer) : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult result || result.Value is ApiError)
            return;

        var status = result switch
        {
            BadRequestObjectResult => StatusCodes.Status400BadRequest,
            NotFoundObjectResult => StatusCodes.Status404NotFound,
            UnauthorizedObjectResult => StatusCodes.Status401Unauthorized,
            ConflictObjectResult => StatusCodes.Status409Conflict,
            UnprocessableEntityObjectResult => StatusCodes.Status422UnprocessableEntity,
            _ => result.StatusCode ?? (result.Value as ProblemDetails)?.Status
        };
        if (status is null or < 400)
            return;

        var message = status >= 500
            ? "No se pudo completar la solicitud."
            : ExtractMessage(result.Value) ?? DefaultMessage(status.Value);
        var code = status.Value switch
        {
            400 => "VALIDATION_ERROR",
            401 => "UNAUTHORIZED",
            403 => "FORBIDDEN",
            404 => "NOT_FOUND",
            409 => "CONFLICT",
            429 => "RATE_LIMITED",
            >= 500 => "INTERNAL_ERROR",
            _ => "HTTP_ERROR"
        };
        int? retryAfter = int.TryParse(context.HttpContext.Response.Headers["Retry-After"], out var seconds)
            ? seconds
            : null;

        context.Result = new ObjectResult(new ApiError(
            code,
            localizer.LocalizeError(message, context.HttpContext.Request.Headers.AcceptLanguage.ToString()),
            context.HttpContext.TraceIdentifier,
            retryAfter))
        {
            StatusCode = status
        };
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static string? ExtractMessage(object? value)
    {
        if (value is null) return null;
        if (value is ProblemDetails problem)
            return FirstNonEmpty(problem.Detail, problem.Title);
        if (value is string text)
            return string.IsNullOrWhiteSpace(text) ? null : text;
        if (value is JsonElement json && json.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in json.EnumerateObject())
            {
                if (!IsMessageProperty(property.Name)) continue;
                var candidate = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : null;
                if (!string.IsNullOrWhiteSpace(candidate)) return candidate;
            }
        }
        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is string key && IsMessageProperty(key) && entry.Value is string candidate &&
                    !string.IsNullOrWhiteSpace(candidate)) return candidate;
            }
        }

        foreach (var name in new[] { "Message", "Error", "Detail", "Title" })
        {
            var candidate = value.GetType().GetProperty(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
                ?.GetValue(value) as string;
            if (!string.IsNullOrWhiteSpace(candidate)) return candidate;
        }
        return null;
    }

    private static bool IsMessageProperty(string name) =>
        name.Equals("message", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("error", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("detail", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("title", StringComparison.OrdinalIgnoreCase);

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string DefaultMessage(int status) => status switch
    {
        400 => "La solicitud no es válida.",
        401 => "No se pudo identificar al usuario autenticado.",
        403 => "No tienes permiso para realizar esta acción.",
        404 => "El recurso solicitado no existe.",
        409 => "La solicitud entra en conflicto con el estado actual.",
        429 => "Se excedió el límite de solicitudes. Inténtalo de nuevo más tarde.",
        _ => "No se pudo completar la solicitud."
    };
}
