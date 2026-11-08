namespace Api.Modules.Files.Policies;

internal enum FileUploadValidationFailure
{
    None,
    FileRequired,
    FileTooLarge,
    FileTypeNotAllowed
}

internal static class FileUploadPolicy
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedMediaTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".doc"] = ["application/msword"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xls"] = ["application/vnd.ms-excel"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".ppt"] = ["application/vnd.ms-powerpoint"],
            [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
            [".odt"] = ["application/vnd.oasis.opendocument.text"],
            [".ods"] = ["application/vnd.oasis.opendocument.spreadsheet"],
            [".odp"] = ["application/vnd.oasis.opendocument.presentation"],
            [".rtf"] = ["application/rtf", "text/rtf"],
            [".txt"] = ["text/plain"],
            [".csv"] = ["text/csv", "application/csv"],
            [".md"] = ["text/markdown", "text/plain"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"],
            [".gif"] = ["image/gif"],
            [".webp"] = ["image/webp"],
            [".bmp"] = ["image/bmp"],
            [".tif"] = ["image/tiff"],
            [".tiff"] = ["image/tiff"],
            [".heic"] = ["image/heic"],
            [".heif"] = ["image/heif"],
            [".mp3"] = ["audio/mpeg"],
            [".wav"] = ["audio/wav", "audio/x-wav"],
            [".m4a"] = ["audio/mp4", "audio/x-m4a"],
            [".aac"] = ["audio/aac"],
            [".ogg"] = ["audio/ogg"],
            [".flac"] = ["audio/flac"],
            [".mp4"] = ["video/mp4"],
            [".m4v"] = ["video/mp4", "video/x-m4v"],
            [".mov"] = ["video/quicktime"],
            [".webm"] = ["video/webm", "audio/webm"],
            [".avi"] = ["video/x-msvideo"],
            [".mpeg"] = ["video/mpeg"],
            [".mpg"] = ["video/mpeg"],
            [".zip"] = ["application/zip", "application/x-zip-compressed"]
        };

    public static FileUploadValidationFailure Validate(IFormFile? file, long maximumSizeBytes)
    {
        if (file is null || file.Length == 0)
            return FileUploadValidationFailure.FileRequired;
        if (file.Length > maximumSizeBytes)
            return FileUploadValidationFailure.FileTooLarge;

        string extension = Path.GetExtension(file.FileName);
        string contentType = NormalizeMediaType(file.ContentType);
        return !string.IsNullOrWhiteSpace(extension) &&
            AllowedMediaTypes.TryGetValue(extension, out string[]? allowed) &&
            allowed.Contains(contentType, StringComparer.OrdinalIgnoreCase)
                ? FileUploadValidationFailure.None
                : FileUploadValidationFailure.FileTypeNotAllowed;
    }

    public static async Task<bool> HasValidSignatureAsync(IFormFile file, CancellationToken cancellationToken)
    {
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using Stream stream = file.OpenReadStream();
        byte[] header = new byte[16];
        int read = await stream.ReadAsync(header, cancellationToken);
        ReadOnlySpan<byte> bytes = header.AsSpan(0, read);

        return extension switch
        {
            ".pdf" => StartsWith(bytes, "%PDF-"u8),
            ".jpg" or ".jpeg" => StartsWith(bytes, [0xFF, 0xD8, 0xFF]),
            ".png" => StartsWith(bytes, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            ".gif" => StartsWith(bytes, "GIF87a"u8) || StartsWith(bytes, "GIF89a"u8),
            ".bmp" => StartsWith(bytes, "BM"u8),
            ".tif" or ".tiff" => StartsWith(bytes, [0x49, 0x49, 0x2A, 0x00]) || StartsWith(bytes, [0x4D, 0x4D, 0x00, 0x2A]),
            ".zip" or ".docx" or ".xlsx" or ".pptx" or ".odt" or ".ods" or ".odp" =>
                StartsWith(bytes, [0x50, 0x4B, 0x03, 0x04]) || StartsWith(bytes, [0x50, 0x4B, 0x05, 0x06]),
            ".wav" => StartsWith(bytes, "RIFF"u8) && At(bytes, 8, "WAVE"u8),
            ".avi" => StartsWith(bytes, "RIFF"u8) && At(bytes, 8, "AVI "u8),
            ".webp" => StartsWith(bytes, "RIFF"u8) && At(bytes, 8, "WEBP"u8),
            ".flac" => StartsWith(bytes, "fLaC"u8),
            ".ogg" => StartsWith(bytes, "OggS"u8),
            ".mp3" => StartsWith(bytes, "ID3"u8) || read >= 2 && bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0,
            ".mp4" or ".m4v" or ".m4a" or ".mov" => At(bytes, 4, "ftyp"u8),
            _ => true
        };
    }

    private static bool StartsWith(ReadOnlySpan<byte> value, ReadOnlySpan<byte> prefix) => value.StartsWith(prefix);

    private static bool At(ReadOnlySpan<byte> value, int offset, ReadOnlySpan<byte> expected) =>
        value.Length >= offset + expected.Length && value[offset..].StartsWith(expected);

    private static string NormalizeMediaType(string contentType)
    {
        int separator = contentType.IndexOf(';', StringComparison.Ordinal);
        return (separator >= 0 ? contentType[..separator] : contentType).Trim();
    }
}
