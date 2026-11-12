using System.Net;

namespace Api.Configuration;

internal static class CertificateIdentityCatalog
{
    public const string FriendlyHostName = "turbo-deck.local";

    public static string[] GetSuggestedIdentities(IEnumerable<string> listenerAddresses)
    {
        var candidates = new List<string> { FriendlyHostName, "localhost", "127.0.0.1", "::1" };
        candidates.AddRange(listenerAddresses);
        candidates.AddRange(NetworkAddressCatalog.GetSuggestions());
        if (!string.IsNullOrWhiteSpace(Environment.MachineName))
            candidates.Add(Environment.MachineName);
        return NormalizeDistinct(candidates);
    }

    public static string[] NormalizeDistinct(IEnumerable<string> values)
    {
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string value in values)
        {
            string candidate = value.Trim();
            if (candidate.Length == 0 || candidate is "*" or "+")
                continue;

            if (string.Equals(candidate, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                identities.Add("localhost");
                continue;
            }

            if (IPAddress.TryParse(candidate, out IPAddress? address))
            {
                if (!NetworkAddressCatalog.IsWildcard(address))
                    identities.Add(address.ToString());
                continue;
            }

            if (Uri.CheckHostName(candidate) == UriHostNameType.Dns)
                identities.Add(candidate.ToLowerInvariant());
        }
        return identities.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
