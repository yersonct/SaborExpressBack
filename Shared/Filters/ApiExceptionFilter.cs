using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaborExpress.Modules.Auth.Exceptions;
using SaborExpress.Shared.Exceptions;

namespace SaborExpress.Shared.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var result = context.Exception switch
        {
            TooManyLoginAttemptsException ex => BuildRetryAfterResult(context, ex.RetryAfterSeconds, ex.Message),
            TooManyResetAttemptsException ex => BuildRetryAfterResult(context, ex.RetryAfterSeconds, ex.Message),
            EmailNotConfirmedException ex => new ObjectResult(new { error = ex.Message, code = "EMAIL_NOT_CONFIRMED" }) { StatusCode = 401 },
            AccountNotActivatedException ex => new ObjectResult(new { error = ex.Message, code = "ACCOUNT_NOT_ACTIVATED" }) { StatusCode = 401 },
            ForbiddenException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 403 },
            UnauthorizedAccessException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 401 },
            KeyNotFoundException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 404 },
            PersistenceException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 500 },
            InvalidOperationException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 409 },
            ArgumentException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 400 },
            _ => new ObjectResult(new { error = "No se pudo completar la solicitud." }) { StatusCode = 400 }
        };

        context.Result = result;
        context.ExceptionHandled = true;
    }

    private ObjectResult BuildRetryAfterResult(
        ExceptionContext context,
        int retryAfterSeconds,
        string message)
    {
        context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
        return new ObjectResult(new
        {
            message,
            retryAfterSeconds
        }) { StatusCode = 429 };
    }
}
