using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace LocalDrop.Services;

public static class NetUtil
{
    /// <summary>True when the address is loopback or in a private / link-local range (RFC 1918, 4193, 3927, fe80::/10).</summary>
    public static bool IsLan(IPAddress? address)
    {
        if (address is null) return false;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] == 10
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 169 && b[1] == 254)
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127); // CGNAT, used by some phone hotspots
        }
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal) return true;
            var b = address.GetAddressBytes();
            return (b[0] & 0xFE) == 0xFC; // fc00::/7 unique local
        }
        return false;
    }

    /// <summary>Best-guess LAN IPv4 addresses of this device, preferring Wi-Fi/Ethernet that are up.</summary>
    public static List<IPAddress> LocalIPv4Addresses()
    {
        var result = new List<(int rank, IPAddress ip)>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var name = nic.Name + " " + nic.Description;
                if (name.Contains("vEthernet", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("WSL", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Docker", StringComparison.OrdinalIgnoreCase))
                    continue;

                int rank = nic.NetworkInterfaceType switch
                {
                    NetworkInterfaceType.Wireless80211 => 0,
                    NetworkInterfaceType.Ethernet => 1,
                    _ => 2,
                };
                foreach (var ua in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (!IsLan(ua.Address) || IPAddress.IsLoopback(ua.Address)) continue;
                    var b = ua.Address.GetAddressBytes();
                    if (b[0] == 169 && b[1] == 254) rank += 10; // APIPA last
                    result.Add((rank, ua.Address));
                }
            }
        }
        catch { /* fall through to socket trick */ }

        if (result.Count == 0)
        {
            try
            {
                using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                s.Connect("10.255.255.255", 1);
                if (s.LocalEndPoint is IPEndPoint ep && IsLan(ep.Address)) result.Add((0, ep.Address));
            }
            catch { }
        }
        return result.OrderBy(r => r.rank).Select(r => r.ip).Distinct().ToList();
    }

    public static int FindFreePort(int preferred, int attempts = 20)
    {
        for (int p = preferred; p < preferred + attempts; p++)
        {
            try
            {
                using var l = new TcpListener(IPAddress.Any, p);
                l.Start();
                l.Stop();
                return p;
            }
            catch { }
        }
        return preferred;
    }

    private static readonly char[] Invalid = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).Distinct().ToArray();

    /// <summary>Makes a user-supplied name safe for use as a single path segment.</summary>
    public static string SanitizeSegment(string? raw, string fallback, int maxLen = 80)
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw.Trim())
            sb.Append(Array.IndexOf(Invalid, c) >= 0 || char.IsControl(c) ? '_' : c);
        var s = sb.ToString().Trim(' ', '.');
        if (s.Length == 0 || s == "." || s == "..") return fallback;
        if (s.Length > maxLen) s = s[..maxLen];
        return s;
    }

    /// <summary>Keeps extension when truncating/sanitising a file name.</summary>
    public static string SanitizeFileName(string? raw)
    {
        var name = SanitizeSegment(raw, "file", 180);
        return name;
    }
}
