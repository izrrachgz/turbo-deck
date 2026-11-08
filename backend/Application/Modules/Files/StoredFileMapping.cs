using Domain.Modules.Files;

namespace Application.Modules.Files;

internal static class StoredFileMapping
{
    public static StoredFileModel ToModel(this StoredFile file) => new(
        file.Id,
        file.OriginalFileName,
        file.ContentType,
        file.Extension,
        file.Size,
        file.Sha256,
        file.Purpose,
        file.CreatedUtc,
        file.ModifiedUtc);
}
