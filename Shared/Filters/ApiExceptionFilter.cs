using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaborExpress.Modules.Auth.Exceptions;
using SaborExpress.Shared.Exceptions;
using SaborExpress.Shared.Responses;

namespace SaborExpress.Shared.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    public ApiExceptionFilter(ILogger<ApiExceptionFilter>? logger = null) =>
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ApiExceptionFilter>.Instance;

    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;
        var (status, code, message) = ex switch
        {
            TooManyLoginAttemptsException => (429, "RATE_LIMITED", ex.Message),
            TooManyResetAttemptsException => (429, "RATE_LIMITED", ex.Message),
            EmailNotConfirmedException => (401, "EMAIL_NOT_CONFIRMED", ex.Message),
            AccountNotActivatedException => (401, "ACCOUNT_NOT_ACTIVATED", ex.Message),
            ForbiddenException => (403, "FORBIDDEN", ex.Message),
            UnauthorizedAccessException => (401, "UNAUTHORIZED", ex.Message),
            KeyNotFoundException or NotFoundException => (404, "NOT_FOUND", ex.Message),
            ArgumentException or BadRequestException => (400, "VALIDATION_ERROR", ex.Message),
            ConflictException => (409, "CONFLICT", ex.Message),
            InvalidOperationException => (500, "INTERNAL_ERROR", "No se pudo completar la solicitud."),
            _ => (500, "INTERNAL_ERROR", "No se pudo completar la solicitud.")
        };
        int? retryAfter = ex switch
        {
            TooManyLoginAttemptsException rate => rate.RetryAfterSeconds,
            TooManyResetAttemptsException reset => reset.RetryAfterSeconds,
            _ => null
        };
        if (retryAfter.HasValue)
            context.HttpContext.Response.Headers["Retry-After"] = retryAfter.Value.ToString();
        if (status >= 500)
            _logger.LogError(ex, "API failure. TraceId {TraceId}", context.HttpContext.TraceIdentifier);
        context.Result = new ObjectResult(new ApiError(code, message,
            context.HttpContext.TraceIdentifier, retryAfter)) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
