using System.Net;
using System.Net.Sockets;

namespace SaborExpress.Shared.Security;

public static class CorsOriginPolicy
{
    public static HashSet<string> FilterConfiguredOrigins(
        IEnumerable<string> configuredOrigins,
        bool isDevelopment)
    {
        return configuredOrigins
            .Where(origin => IsValidOrigin(origin) &&
                (isDevelopment || !IsDevelopmentOrigin(origin)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsAllowedOrigin(
        string origin,
        IReadOnlySet<string> allowedOrigins,
        bool isDevelopment) =>
        allowedOrigins.Contains(origin) ||
        (isDevelopment && IsDevelopmentOrigin(origin));

    public static bool IsValidOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        return string.Equals(
            uri.GetLeftPart(UriPartial.Authority),
            origin.TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDevelopmentOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        var host = uri.DnsSafeHost;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".devtunnels.ms", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(host, out var address))
            return false;
        if (IPAddress.IsLoopback(address))
            return true;

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || (bytes[0] & 0xfe) == 0xfc;
        }

        return bytes[0] == 0 ||
            bytes[0] == 10 ||
            bytes[0] == 127 ||
            (bytes[0] == 169 && bytes[1] == 254) ||
            (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
            (bytes[0] == 192 && bytes[1] == 168) ||
            (bytes[0] == 100 && bytes[1] is >= 64 and <= 127);
    }
}
