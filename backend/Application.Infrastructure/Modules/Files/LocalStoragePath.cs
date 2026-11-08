namespace Application.Infrastructure.Modules.Files;

internal static class LocalStoragePath
{
    public static string ResolveRoot(FileStorageOptions options)
    {
        string dataDirectory = Path.GetFullPath(options.DataDirectory);
        string root = Path.IsPathRooted(options.RootPath)
            ? Path.GetFullPath(options.RootPath)
            : Path.GetFullPath(Path.Combine(dataDirectory, options.RootPath));

        return root;
    }

    public static bool TryResolveFile(string root, string storageName, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(storageName) ||
            Path.IsPathRooted(storageName) ||
            !string.Equals(Path.GetFileName(storageName), storageName, StringComparison.Ordinal))
        {
            return false;
        }

        string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(Path.Combine(normalizedRoot, storageName));
        if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return false;

        path = candidate;
        return true;
    }
}
