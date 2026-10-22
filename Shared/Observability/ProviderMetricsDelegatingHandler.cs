using System.Diagnostics;

namespace SaborExpress.Shared.Observability;

/// <summary>Measures an outbound dependency without capturing request URLs, headers or bodies.</summary>
public sealed class ProviderMetricsDelegatingHandler : DelegatingHandler
{
    private readonly ApiMetricsRegistry _metrics;
    private readonly string _provider;

    public ProviderMetricsDelegatingHandler(ApiMetricsRegistry metrics, string provider)
    {
        _metrics = metrics;
        _provider = provider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var statusCode = 0;
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            statusCode = (int)response.StatusCode;
            return response;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordProviderRequest(_provider, statusCode, stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
