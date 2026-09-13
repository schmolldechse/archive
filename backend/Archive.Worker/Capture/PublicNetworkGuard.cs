using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Archive.Worker.Capture;

// Application-level SSRF guard. Production deployments must additionally deny
// private/link-local destinations at the network boundary to close DNS rebinding
// and proxy-related gaps outside the browser process.
internal sealed class PublicNetworkGuard
{
    private readonly ConcurrentDictionary<string, Task<IPAddress[]>> _lookups =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<bool> IsAllowedAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
            return false;

        if (IPAddress.TryParse(uri.DnsSafeHost, out var literal))
            return IsPublic(literal);

        IPAddress[] addresses;
        try
        {
            addresses = await _lookups.GetOrAdd(uri.IdnHost, static host => Dns.GetHostAddressesAsync(host))
                .WaitAsync(cancellationToken);
        }
        catch (SocketException)
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(IsPublic);
    }

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] switch
            {
                0 or 10 or 127 => false,
                100 when bytes[1] is >= 64 and <= 127 => false,
                169 when bytes[1] == 254 => false,
                172 when bytes[1] is >= 16 and <= 31 => false,
                192 when bytes[1] == 0 && bytes[2] == 0 => false,
                192 when bytes[1] == 0 && bytes[2] == 2 => false,
                192 when bytes[1] == 88 && bytes[2] == 99 => false,
                192 when bytes[1] == 168 => false,
                198 when bytes[1] is 18 or 19 => false,
                198 when bytes[1] == 51 && bytes[2] == 100 => false,
                203 when bytes[1] == 0 && bytes[2] == 113 => false,
                >= 224 => false,
                _ => true
            };
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6 ||
            address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6Loopback) ||
            address.Equals(IPAddress.IPv6None) || address.IsIPv6LinkLocal ||
            address.IsIPv6Multicast || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal)
            return false;

        // Deprecated IPv4-compatible form (::a.b.c.d).
        if (bytes[..12].All(value => value == 0))
            return IsPublic(new IPAddress(bytes[12..]));

        // IETF discard-only and documentation prefixes.
        if (bytes[0] == 0x01 && bytes[1] == 0x00 && bytes[2..8].All(value => value == 0) ||
            bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8)
            return false;

        return true;
    }
}
