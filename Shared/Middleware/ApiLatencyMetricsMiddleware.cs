using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Routing;
using SaborExpress.Shared.Observability;

namespace SaborExpress.Shared.Middleware;

/// <summary>
/// Registra la latencia de procesamiento de la API por ruta estable y método.
/// Un exportador de métricas puede calcular el p95 a partir de este histograma.
/// </summary>
public sealed class ApiLatencyMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiMetricsRegistry _metrics;
    private readonly ApiRouteCatalog _routeCatalog;

    public ApiLatencyMetricsMiddleware(
        RequestDelegate next,
        ApiMetricsRegistry metrics,
        ApiRouteCatalog routeCatalog)
    {
        _next = next;
        _metrics = metrics;
        _routeCatalog = routeCatalog;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var route = GetRouteTemplate(context);
        double? responseStartDurationMs = null;
        context.Response.OnStarting(() =>
        {
            responseStartDurationMs = stopwatch.Elapsed.TotalMilliseconds;
            var duration = responseStartDurationMs.Value.ToString("F2", CultureInfo.InvariantCulture);
            context.Response.Headers["Server-Timing"] = $"app;dur={duration};desc=\"{route}\"";
            return Task.CompletedTask;
        });
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
                !context.Request.Path.StartsWithSegments("/api/telemetry/client-latency", StringComparison.OrdinalIgnoreCase))
            {
                _routeCatalog.RegisterTrustedRouteTemplate(route);
                _metrics.RecordServerRequest(
                    context.Request.Method,
                    route,
                    context.Response.StatusCode,
                    responseStartDurationMs ?? stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }

    private static string GetRouteTemplate(HttpContext context) =>
        ApiMetricsRegistry.NormalizeRouteTemplate(
            (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText);
}
