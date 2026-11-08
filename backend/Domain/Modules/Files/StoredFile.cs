using Domain.Entities;

namespace Domain.Modules.Files;

/// <summary>
/// Describes a file stored by the application without assigning business ownership.
/// </summary>
public sealed class StoredFile : BaseEntity
{
    public required string OriginalFileName { get; set; }

    public required string StorageName { get; set; }

    public required string ContentType { get; set; }

    public required string Extension { get; set; }

    public long Size { get; set; }

    public required string Sha256 { get; set; }

    public string? Purpose { get; set; }
}
