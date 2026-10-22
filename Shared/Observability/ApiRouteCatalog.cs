using System.Collections.Concurrent;

namespace SaborExpress.Shared.Observability;

/// <summary>Allow-list of route templates registered by this API, used for client timing reports.</summary>
public sealed class ApiRouteCatalog
{
    private readonly ConcurrentDictionary<string, byte> _routes = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterTrustedRouteTemplate(string route)
    {
        var normalized = ApiMetricsRegistry.NormalizeRouteTemplate(route);
        if (normalized.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            _routes.TryAdd(normalized, 0);
    }

    public bool Contains(string? route) =>
        route is not null && _routes.ContainsKey(ApiMetricsRegistry.NormalizeRouteTemplate(route));
}
