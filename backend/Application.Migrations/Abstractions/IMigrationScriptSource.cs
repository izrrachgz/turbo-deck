using Common.Enumerations;
using Application.Migrations.Models;

namespace Application.Migrations.Abstractions
{
    /// <summary>
    /// Supplies application migration scripts for a configured database engine.
    /// </summary>
    internal interface IMigrationScriptSource
    {
        Task<IReadOnlyList<MigrationScript>> GetScriptsAsync(DatabaseEngine engine, CancellationToken cancellationToken = default);
    }
}
