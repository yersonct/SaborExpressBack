using Microsoft.AspNetCore.Mvc.Filters;
using SaborExpress.Shared.Responses;
namespace SaborExpress.Shared.Filters;

public sealed class ApiPaginationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var page in context.ActionArguments.Values.OfType<PageQuery>())
        {
            if (context.HttpContext.Request.Path.StartsWithSegments("/api/v1"))
            { page.Page ??= 1; page.PageSize ??= 100; }
            if (!page.IsRequested) continue;
            context.HttpContext.Response.Headers["X-Page"] = (page.Page ?? 1).ToString();
            context.HttpContext.Response.Headers["X-Page-Size"] = page.Limit.ToString();
        }
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}
