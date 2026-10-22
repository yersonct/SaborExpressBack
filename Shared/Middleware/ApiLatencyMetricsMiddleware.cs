using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.AspNetCore.Routing;

namespace SaborExpress.Shared.Middleware;

/// <summary>
/// Registra la latencia de procesamiento de la API por ruta estable y método.
/// Un exportador de métricas puede calcular el p95 a partir de este histograma.
/// </summary>
public sealed class ApiLatencyMetricsMiddleware
{
    private static readonly Meter Meter = new("SaborExpress.Api", "1.0.0");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "sabor_express.http.server.request.duration",
        unit: "ms",
        description: "Tiempo de procesamiento HTTP del servidor; no incluye la latencia de red del cliente.");

    private readonly RequestDelegate _next;

    public ApiLatencyMetricsMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Server-Timing"] =
                $"app;dur={stopwatch.Elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture)}";
            return Task.CompletedTask;
        });
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            var tags = new TagList
            {
                { "http.request.method", context.Request.Method },
                { "http.route", route },
                { "http.response.status_code", context.Response.StatusCode },
            };

            RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
        }
    }
}
