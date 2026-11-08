using System.Text;
using System.Reflection;
using Common.Enumerations;
using System.Security.Cryptography;
using Application.Migrations.Models;
using Application.Migrations.Abstractions;

namespace Application.Migrations.Services
{
    internal sealed class EmbeddedMigrationScriptSource : IMigrationScriptSource
    {
        private const string MigrationFilePrefix = "migration-";
        private const string SqlFileExtension = ".sql";
        private readonly Assembly _assembly = typeof(ApplicationMigrationAssembly).Assembly;

        public async Task<IReadOnlyList<MigrationScript>> GetScriptsAsync(
            DatabaseEngine engine,
            CancellationToken cancellationToken = default)
        {
            string resourcePrefix = $"{_assembly.GetName().Name}.Migrations.{engine}.";
            var scripts = new List<MigrationScript>();

            foreach (string resourceName in _assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal))
                .Order(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var metadata = ParseMetadata(resourcePrefix, resourceName);

                await using var stream = _assembly.GetManifestResourceStream(resourceName) ??
                    throw new InvalidOperationException($"Embedded migration resource '{resourceName}' could not be opened.");
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                string content = await reader.ReadToEndAsync(cancellationToken);
                string checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

                scripts.Add(new MigrationScript(
                    metadata.MigrationId,
                    metadata.Module,
                    metadata.Name,
                    resourceName,
                    content,
                    checksum));
            }

            return scripts
                .OrderBy(script => script.MigrationId)
                .ToArray();
        }

        private static (long MigrationId, string Module, string Name) ParseMetadata(string resourcePrefix, string resourceName)
        {
            string relativeName = resourceName[resourcePrefix.Length..];
            string[] segments = relativeName.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 3 || !string.Equals(segments[^1], "sql", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Embedded migration resource '{resourceName}' does not follow the expected module/migration-X.sql convention.");
            }

            string module = string.Join('.', segments[..^2]);
            string migrationName = segments[^2];

            if (!migrationName.StartsWith(MigrationFilePrefix, StringComparison.OrdinalIgnoreCase) ||
                !long.TryParse(migrationName[MigrationFilePrefix.Length..], out long migrationId))
            {
                throw new InvalidOperationException($"Embedded migration resource '{resourceName}' does not contain a valid numeric migration ID.");
            }

            return (migrationId, module, migrationName + SqlFileExtension);
        }
    }
}
