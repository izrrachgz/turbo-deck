using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Api.Configuration;

internal static class NetworkAddressCatalog
{
    public static bool TryParse(string? value, out IPAddress address)
    {
        address = IPAddress.None;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string candidate = value.Trim();
        address = candidate.ToLowerInvariant() switch
        {
            "localhost" or "127.0.0.1" => IPAddress.Loopback,
            "::1" => IPAddress.IPv6Loopback,
            "*" or "+" or "0.0.0.0" => IPAddress.Any,
            "::" => IPAddress.IPv6Any,
            _ => IPAddress.None
        };
        return !address.Equals(IPAddress.None) || IPAddress.TryParse(candidate, out address!);
    }

    public static bool IsWildcard(IPAddress address) => address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any);

    public static string[] NormalizeDistinct(IEnumerable<string> values)
    {
        var parsedAddresses = new HashSet<IPAddress>();
        var invalidAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<string>();
        foreach (string value in values)
        {
            string candidate = value.Trim();
            if (candidate.Length == 0)
                continue;

            bool added = TryParse(candidate, out IPAddress parsed)
                ? parsedAddresses.Add(parsed)
                : invalidAddresses.Add(candidate);
            if (added)
                normalized.Add(candidate);
        }
        return normalized.ToArray();
    }

    public static string[] GetSuggestions()
    {
        var suggestions = new List<string> { "localhost", "127.0.0.1", "0.0.0.0" };
        try
        {
            IEnumerable<string> currentAddresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == OperationalStatus.Up &&
                    network.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
                .SelectMany(network => network.GetIPProperties().UnicastAddresses)
                .Select(address => address.Address)
                .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                .Select(address => address.ToString())
                .Order(StringComparer.OrdinalIgnoreCase);
            suggestions.AddRange(currentAddresses);
        }
        catch (NetworkInformationException)
        {
            // Fixed local presets remain available when adapter enumeration is unavailable.
        }
        catch (SocketException)
        {
            // Fixed local presets remain available when adapter enumeration is unavailable.
        }

        return suggestions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
