using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;

namespace SaborExpress.Shared.Observability;

/// <summary>
/// Aggregates bounded-cardinality latency series and exposes the same measurements
/// through System.Diagnostics.Metrics and the Prometheus scrape endpoint.
/// </summary>
public sealed class ApiMetricsRegistry : IDisposable
{
    private const int MaxRequestSeries = 2_000;
    private const int MaxProviderSeries = 2_000;
    private static readonly double[] BucketsMs =
        [5, 10, 25, 50, 100, 250, 500, 1_000, 2_500, 5_000, 10_000, 30_000, 60_000, 120_000];
    private static readonly HashSet<string> AllowedProviders =
        ["geocoding", "wompi", "email", "push"];
    private static readonly HashSet<string> AllowedMethods =
        ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE", "OTHER"];

    private readonly ConcurrentDictionary<RequestSeriesKey, HistogramSeries> _serverRequests = new();
    private readonly ConcurrentDictionary<RequestSeriesKey, HistogramSeries> _clientNetwork = new();
    private readonly ConcurrentDictionary<ProviderSeriesKey, HistogramSeries> _providerRequests = new();
    private readonly object _requestSeriesLock = new();
    private readonly object _clientSeriesLock = new();
    private readonly object _providerSeriesLock = new();
    private readonly Meter _meter = new("SaborExpress.Api", "1.0.0");
    private readonly Histogram<double> _serverDurationInstrument;
    private readonly Counter<long> _serverRequestsInstrument;
    private readonly Histogram<double> _clientNetworkInstrument;
    private readonly Histogram<double> _providerDurationInstrument;
    private readonly Counter<long> _providerRequestsInstrument;

    public ApiMetricsRegistry()
    {
        _serverDurationInstrument = _meter.CreateHistogram<double>(
            "sabor_express.http.server.request.duration", "ms",
            "HTTP request processing time measured by the API server.");
        _serverRequestsInstrument = _meter.CreateCounter<long>(
            "sabor_express.http.server.requests", "requests",
            "HTTP requests completed by the API server.");
        _clientNetworkInstrument = _meter.CreateHistogram<double>(
            "sabor_express.http.client.network.duration", "ms",
            "Client-observed request time excluding the server duration reported by Server-Timing.");
        _providerDurationInstrument = _meter.CreateHistogram<double>(
            "sabor_express.provider.request.duration", "ms",
            "HTTP dependency request duration measured by the API server.");
        _providerRequestsInstrument = _meter.CreateCounter<long>(
            "sabor_express.provider.requests", "requests",
            "HTTP dependency requests completed by the API server.");
    }

    public void RecordServerRequest(string method, string route, int statusCode, double durationMs)
    {
        var key = NormalizeRequestKey(method, route, statusCode);
        GetOrCreateRequestSeries(_serverRequests, _requestSeriesLock, key, MaxRequestSeries)
            .Record(durationMs);

        var tags = new TagList
        {
            { "http.request.method", key.Method },
            { "http.route", key.Route },
            { "http.response.status_code", key.StatusCode },
        };
        _serverDurationInstrument.Record(durationMs, tags);
        _serverRequestsInstrument.Add(1, tags);
    }

    public void RecordClientNetwork(string method, string route, int statusCode, double durationMs)
    {
        var key = NormalizeRequestKey(method, route, statusCode);
        GetOrCreateRequestSeries(_clientNetwork, _clientSeriesLock, key, MaxRequestSeries)
            .Record(durationMs);

        var tags = new TagList
        {
            { "http.request.method", key.Method },
            { "http.route", key.Route },
            { "http.response.status_code", key.StatusCode },
            { "client.platform", "admin_web" },
        };
        _clientNetworkInstrument.Record(durationMs, tags);
    }

    public void RecordProviderRequest(string provider, int statusCode, double durationMs)
    {
        provider = AllowedProviders.Contains(provider) ? provider : "other";
        statusCode = NormalizeStatusCode(statusCode);
        var key = new ProviderSeriesKey(provider, statusCode);
        GetOrCreateProviderSeries(key).Record(durationMs);

        var tags = new TagList
        {
            { "provider", provider },
            { "http.response.status_code", statusCode },
        };
        _providerDurationInstrument.Record(durationMs, tags);
        _providerRequestsInstrument.Add(1, tags);
    }

    public string RenderPrometheus()
    {
        var output = new StringBuilder(8_192);
        AppendRequestMetrics(output, _serverRequests,
            "sabor_express_http_server_requests_total",
            "Completed API requests, grouped by HTTP method, route template and status code.");
        AppendHistogram(output, _serverRequests,
            "sabor_express_http_server_request_duration_milliseconds",
            "Server-side API request duration in milliseconds.");
        AppendRequestMetrics(output, _clientNetwork,
            "sabor_express_http_client_network_requests_total",
            "Client-observed API requests with a valid Server-Timing measurement.");
        AppendHistogram(output, _clientNetwork,
            "sabor_express_http_client_network_duration_milliseconds",
            "Client-observed request duration minus API server duration, in milliseconds.");
        AppendProviderMetrics(output);
        return output.ToString();
    }

    public static string NormalizeMethod(string? method)
    {
        var normalized = method?.Trim().ToUpperInvariant();
        return normalized is not null && AllowedMethods.Contains(normalized) ? normalized : "OTHER";
    }

    public static int NormalizeStatusCode(int statusCode) => statusCode is >= 100 and <= 599 ? statusCode : 0;

    public static string NormalizeRouteTemplate(string? route)
    {
        if (string.IsNullOrWhiteSpace(route) || route.Length > 256)
            return "__other__";

        // RouteEndpoint templates are static. Reject values that look like concrete URLs or paths.
        if (route.Contains("?", StringComparison.Ordinal) || route.Contains('#') || route.Contains("://", StringComparison.Ordinal))
            return "__other__";

        return route.StartsWith('/') ? route : $"/{route}";
    }

    public void Dispose() => _meter.Dispose();

    private static RequestSeriesKey NormalizeRequestKey(string method, string route, int statusCode) =>
        new(NormalizeMethod(method), NormalizeRouteTemplate(route), NormalizeStatusCode(statusCode));

    private static HistogramSeries GetOrCreateRequestSeries(
        ConcurrentDictionary<RequestSeriesKey, HistogramSeries> series,
        object gate,
        RequestSeriesKey key,
        int maxSeries)
    {
        if (series.TryGetValue(key, out var existing)) return existing;

        lock (gate)
        {
            if (series.TryGetValue(key, out existing)) return existing;
            if (series.Count < maxSeries)
                return series.GetOrAdd(key, _ => new HistogramSeries(BucketsMs));

            // Route templates are finite, but the overflow key keeps the exporter bounded
            // if new endpoints are added dynamically or the endpoint set is misconfigured.
            var overflowKey = new RequestSeriesKey(key.Method, "__overflow__", key.StatusCode);
            if (series.TryGetValue(overflowKey, out existing)) return existing;
            if (series.Count < maxSeries + (AllowedMethods.Count * 501))
                return series.GetOrAdd(overflowKey, _ => new HistogramSeries(BucketsMs));

            var finalKey = new RequestSeriesKey("OTHER", "__overflow__", 0);
            return series.GetOrAdd(finalKey, _ => new HistogramSeries(BucketsMs));
        }
    }

    private HistogramSeries GetOrCreateProviderSeries(ProviderSeriesKey key)
    {
        if (_providerRequests.TryGetValue(key, out var existing)) return existing;
        lock (_providerSeriesLock)
        {
            if (_providerRequests.TryGetValue(key, out existing)) return existing;
            if (_providerRequests.Count >= MaxProviderSeries)
                key = new ProviderSeriesKey("other", 0);
            return _providerRequests.GetOrAdd(key, _ => new HistogramSeries(BucketsMs));
        }
    }

    private static void AppendRequestMetrics(
        StringBuilder output,
        ConcurrentDictionary<RequestSeriesKey, HistogramSeries> series,
        string metricName,
        string help)
    {
        output.Append("# HELP ").Append(metricName).Append(' ').AppendLine(help);
        output.Append("# TYPE ").Append(metricName).AppendLine(" counter");
        foreach (var pair in series.OrderBy(pair => pair.Key.Route, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.Method, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.StatusCode))
        {
            var snapshot = pair.Value.Snapshot();
            output.Append(metricName).Append('{');
            AppendRequestLabels(output, pair.Key);
            output.Append("} ").AppendLine(snapshot.Count.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void AppendHistogram(
        StringBuilder output,
        ConcurrentDictionary<RequestSeriesKey, HistogramSeries> series,
        string metricName,
        string help)
    {
        output.Append("# HELP ").Append(metricName).Append(' ').AppendLine(help);
        output.Append("# TYPE ").Append(metricName).AppendLine(" histogram");
        foreach (var pair in series.OrderBy(pair => pair.Key.Route, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.Method, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.StatusCode))
        {
            var snapshot = pair.Value.Snapshot();
            for (var index = 0; index < BucketsMs.Length; index++)
            {
                output.Append(metricName).Append("_bucket{");
                AppendRequestLabels(output, pair.Key);
                output.Append(",le=\"").Append(BucketsMs[index].ToString("0.###", CultureInfo.InvariantCulture))
                    .Append("\"} ").AppendLine(snapshot.Buckets[index].ToString(CultureInfo.InvariantCulture));
            }

            output.Append(metricName).Append("_bucket{");
            AppendRequestLabels(output, pair.Key);
            output.Append(",le=\"+Inf\"} ").AppendLine(snapshot.Count.ToString(CultureInfo.InvariantCulture));
            output.Append(metricName).Append("_sum{");
            AppendRequestLabels(output, pair.Key);
            output.Append("} ").AppendLine(snapshot.SumMs.ToString("0.###", CultureInfo.InvariantCulture));
            output.Append(metricName).Append("_count{");
            AppendRequestLabels(output, pair.Key);
            output.Append("} ").AppendLine(snapshot.Count.ToString(CultureInfo.InvariantCulture));
        }
    }

    private void AppendProviderMetrics(StringBuilder output)
    {
        const string requestsName = "sabor_express_provider_requests_total";
        const string durationName = "sabor_express_provider_request_duration_milliseconds";
        output.Append("# HELP ").Append(requestsName).AppendLine(" Completed outbound HTTP requests, grouped by fixed provider name and status code.");
        output.Append("# TYPE ").Append(requestsName).AppendLine(" counter");
        foreach (var pair in _providerRequests.OrderBy(pair => pair.Key.Provider, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.StatusCode))
        {
            var snapshot = pair.Value.Snapshot();
            output.Append(requestsName).Append("{provider=\"").Append(EscapeLabel(pair.Key.Provider))
                .Append("\",status_code=\"").Append(pair.Key.StatusCode.ToString(CultureInfo.InvariantCulture))
                .Append("\"} ").AppendLine(snapshot.Count.ToString(CultureInfo.InvariantCulture));
        }

        output.Append("# HELP ").Append(durationName).AppendLine(" Outbound provider HTTP request duration in milliseconds.");
        output.Append("# TYPE ").Append(durationName).AppendLine(" histogram");
        foreach (var pair in _providerRequests.OrderBy(pair => pair.Key.Provider, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.StatusCode))
        {
            var snapshot = pair.Value.Snapshot();
            for (var index = 0; index < BucketsMs.Length; index++)
            {
                output.Append(durationName).Append("_bucket{provider=\"").Append(EscapeLabel(pair.Key.Provider))
                    .Append("\",status_code=\"").Append(pair.Key.StatusCode.ToString(CultureInfo.InvariantCulture))
                    .Append("\",le=\"").Append(BucketsMs[index].ToString("0.###", CultureInfo.InvariantCulture))
                    .Append("\"} ").AppendLine(snapshot.Buckets[index].ToString(CultureInfo.InvariantCulture));
            }
            output.Append(durationName).Append("_bucket{provider=\"").Append(EscapeLabel(pair.Key.Provider))
                .Append("\",status_code=\"").Append(pair.Key.StatusCode.ToString(CultureInfo.InvariantCulture))
                .Append("\",le=\"+Inf\"} ").AppendLine(snapshot.Count.ToString(CultureInfo.InvariantCulture));
            output.Append(durationName).Append("_sum{provider=\"").Append(EscapeLabel(pair.Key.Provider))
                .Append("\",status_code=\"").Append(pair.Key.StatusCode.ToString(CultureInfo.InvariantCulture))
                .Append("\"} ").AppendLine(snapshot.SumMs.ToString("0.###", CultureInfo.InvariantCulture));
            output.Append(durationName).Append("_count{provider=\"").Append(EscapeLabel(pair.Key.Provider))
                .Append("\",status_code=\"").Append(pair.Key.StatusCode.ToString(CultureInfo.InvariantCulture))
                .Append("\"} ").AppendLine(snapshot.Count.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void AppendRequestLabels(StringBuilder output, RequestSeriesKey key)
    {
        output.Append("method=\"").Append(EscapeLabel(key.Method))
            .Append("\",route=\"").Append(EscapeLabel(key.Route))
            .Append("\",status_code=\"").Append(key.StatusCode.ToString(CultureInfo.InvariantCulture)).Append('"');
    }

    private static string EscapeLabel(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    private readonly record struct RequestSeriesKey(string Method, string Route, int StatusCode);
    private readonly record struct ProviderSeriesKey(string Provider, int StatusCode);

    private sealed class HistogramSeries
    {
        private readonly object _gate = new();
        private readonly double[] _bucketBounds;
        private readonly long[] _buckets;
        private long _count;
        private double _sumMs;

        public HistogramSeries(double[] bucketBounds)
        {
            _bucketBounds = bucketBounds;
            _buckets = new long[bucketBounds.Length];
        }

        public void Record(double valueMs)
        {
            if (!double.IsFinite(valueMs) || valueMs < 0) return;
            lock (_gate)
            {
                _count++;
                _sumMs += valueMs;
                for (var index = 0; index < _bucketBounds.Length; index++)
                {
                    if (valueMs <= _bucketBounds[index]) _buckets[index]++;
                }
            }
        }

        public HistogramSnapshot Snapshot()
        {
            lock (_gate)
                return new HistogramSnapshot(_count, _sumMs, (long[])_buckets.Clone());
        }
    }

    private sealed record HistogramSnapshot(long Count, double SumMs, long[] Buckets);
}
