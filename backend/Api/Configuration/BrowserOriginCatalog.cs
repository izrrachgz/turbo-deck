namespace Api.Configuration;

internal static class BrowserOriginCatalog
{
    public static bool TryNormalize(string? value, out string origin)
    {
        origin = string.Empty;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? uri) ||
            uri.Scheme is not ("http" or "https") ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        origin = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped).TrimEnd('/');
        return origin.Length != 0;
    }

    public static string[] NormalizeDistinct(IEnumerable<string> values)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var origins = new List<string>();
        foreach (string value in values)
        {
            if (TryNormalize(value, out string origin) && keys.Add(origin))
                origins.Add(origin);
        }
        return origins.ToArray();
    }
}
