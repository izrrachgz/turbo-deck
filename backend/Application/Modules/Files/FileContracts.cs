namespace Application.Modules.Files;

public sealed record StoredFileModel(
    long Id,
    string OriginalFileName,
    string ContentType,
    string Extension,
    long Size,
    string Sha256,
    string? Purpose,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);

public sealed record StoredFileContent(StoredFileModel File, Stream Content);

public sealed record FileStorageWriteResult(string StorageName, long Size, string Sha256);
