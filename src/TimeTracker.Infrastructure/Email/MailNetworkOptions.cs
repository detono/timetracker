using System.Net;
using System.Net.Sockets;

namespace TimeTracker.Infrastructure.Email;

/// <summary>
/// Where company mail settings may make this server connect to. By default only public
/// addresses: otherwise an employer could point "SMTP host" at the database or other internal
/// services and use the test email to probe the server's network. Installations that send
/// through a mail server on their own network set <c>Email__AllowInternalHosts=true</c>.
/// </summary>
public class MailNetworkOptions {
    public const string SectionName = "Email";

    public bool AllowInternalHosts { get; set; }
}

public static class NetworkAddresses {
    /// <summary>
    /// Loopback, private (10/8, 172.16/12, 192.168/16, fc00::/7), link-local (incl. cloud
    /// metadata at 169.254.169.254), carrier-grade NAT, unspecified and multicast addresses.
    /// </summary>
    public static bool IsInternal(IPAddress address) {
        if (address.IsIPv4MappedToIPv6) {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)) {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork) {
            var b = address.GetAddressBytes();
            return b[0] == 0 || b[0] == 10 || b[0] == 127
                   || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                   || (b[0] == 192 && b[1] == 168)
                   || (b[0] == 169 && b[1] == 254)
                   || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
                   || b[0] >= 224;
        }

        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast
               || (address.GetAddressBytes()[0] & 0xFE) == 0xFC;
    }
}
