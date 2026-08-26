using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SaborExpress.Modules.Auth.Exceptions;

namespace SaborExpress.Shared.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var result = context.Exception switch
            {
                TooManyLoginAttemptsException ex => BuildRetryAfterResult(context, ex.RetryAfterSeconds, ex.Message),
                TooManyResetAttemptsException ex => BuildRetryAfterResult(context, ex.RetryAfterSeconds, ex.Message),
                UnauthorizedAccessException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 401 },
                KeyNotFoundException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 404 },
                InvalidOperationException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 409 },
                ArgumentException ex => new ObjectResult(new { error = ex.Message }) { StatusCode = 400 },
                _ => new ObjectResult(new { error = context.Exception.Message }) { StatusCode = 400 }
            };

            context.Result = result;
            context.ExceptionHandled = true;
        }

        private static ObjectResult BuildRetryAfterResult(ExceptionContext context, int retryAfterSeconds, string message)
        {
            context.HttpContext.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            return new ObjectResult(new { message, retryAfterSeconds }) { StatusCode = 429 };
        }
    }
}