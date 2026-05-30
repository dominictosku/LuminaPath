using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Configuration;

/// <summary>
/// Builds the <see cref="ForwardedHeadersOptions"/> used when the backend
/// runs behind a TLS-terminating reverse proxy (the documented nginx +
/// Docker Compose topology).
///
/// <para>
/// The framework default for <c>KnownNetworks</c>/<c>KnownProxies</c> is
/// loopback only. In Compose the proxy is a separate container, so its
/// source address is a private Docker-network IP — not loopback — which
/// means the default silently drops <c>X-Forwarded-Proto</c> /
/// <c>X-Forwarded-For</c>. That breaks HTTPS scheme detection (and any
/// HTTPS redirect) and makes per-IP rate limiting key off the proxy.
/// </para>
///
/// <para>
/// We therefore trust the private (RFC1918 + loopback + IPv6 ULA/link-local)
/// ranges by default, which covers Docker bridge networks. A directly
/// internet-exposed backend still sees a public <c>RemoteIpAddress</c> that
/// is outside this set, so spoofed forwarded headers are ignored.
/// Operators can override the trust set explicitly through configuration.
/// </para>
/// </summary>
internal static class ForwardedHeadersConfiguration
{
    private static readonly string[] DefaultTrustedNetworks =
    [
        "127.0.0.0/8",
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16",
        "::1/128",
        "fc00::/7",
        "fe80::/10",
    ];

    public static ForwardedHeadersOptions Build(IConfiguration config)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            // One proxy hop by default (the app's own reverse proxy). Bump
            // ForwardedHeaders:ForwardLimit if there are chained proxies.
            ForwardLimit = config.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1,
        };

        // Replace the loopback-only default with our configurable trust set.
        // KnownIPNetworks (System.Net.IPNetwork) is the .NET 8+ replacement
        // for the deprecated KnownNetworks property.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var entry in ReadList(config, "ForwardedHeaders:KnownNetworks") ?? DefaultTrustedNetworks)
        {
            if (System.Net.IPNetwork.TryParse(entry.Trim(), out var network))
            {
                options.KnownIPNetworks.Add(network);
            }
        }

        foreach (var entry in ReadList(config, "ForwardedHeaders:KnownProxies") ?? [])
        {
            if (IPAddress.TryParse(entry.Trim(), out var address))
            {
                options.KnownProxies.Add(address);
            }
        }

        return options;
    }

    // Accept both an array binding (ForwardedHeaders:KnownNetworks:0=...)
    // and a single delimited string (FORWARDED_HEADERS_KNOWN_NETWORKS=a;b),
    // mirroring how CORS origins are read elsewhere.
    private static string[]? ReadList(IConfiguration config, string key)
    {
        var array = config.GetSection(key).Get<string[]>();
        if (array is { Length: > 0 })
        {
            return array;
        }

        return ConfigurationValues.SplitList(config[key]);
    }
}
