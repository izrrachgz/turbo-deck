namespace Application.Migrations.Models
{
    /// <summary>
    /// Contains one application-owned SQL migration ready for execution.
    /// </summary>
    internal sealed record MigrationScript(
        long MigrationId,
        string Module,
        string Name,
        string ResourceName,
        string Content,
        string Checksum);
}
