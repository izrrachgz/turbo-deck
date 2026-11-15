using Api.Modules.Files.Policies;

namespace Api.Modules.Media;

internal static class MediaUploadPolicy
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".mp3", ".wav", ".ogg", ".m4a",
        ".mp4", ".webm"
    };

    public static FileUploadValidationFailure Validate(IFormFile? file, long maximumSizeBytes)
    {
        FileUploadValidationFailure failure = FileUploadPolicy.Validate(file, maximumSizeBytes);
        if (failure != FileUploadValidationFailure.None)
            return failure;
        return AllowedExtensions.Contains(Path.GetExtension(file!.FileName))
            ? FileUploadValidationFailure.None
            : FileUploadValidationFailure.FileTypeNotAllowed;
    }

    public static FileUploadValidationFailure ValidateImage(IFormFile? file, long maximumSizeBytes)
    {
        FileUploadValidationFailure failure = FileUploadPolicy.Validate(file, maximumSizeBytes);
        if (failure != FileUploadValidationFailure.None)
            return failure;
        return AllowedImageExtensions.Contains(Path.GetExtension(file!.FileName))
            ? FileUploadValidationFailure.None
            : FileUploadValidationFailure.FileTypeNotAllowed;
    }

    public static bool IsAllowedTransfer(string fileName, string contentType, long size, long maximumSizeBytes)
    {
        if (size is < 1 || size > maximumSizeBytes)
            return false;
        string extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
            return false;
        string normalizedType = contentType.Split(';', 2)[0].Trim();
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => normalizedType == "image/jpeg",
            ".png" => normalizedType == "image/png",
            ".gif" => normalizedType == "image/gif",
            ".webp" => normalizedType == "image/webp",
            ".mp3" => normalizedType == "audio/mpeg",
            ".wav" => normalizedType is "audio/wav" or "audio/x-wav",
            ".ogg" => normalizedType == "audio/ogg",
            ".m4a" => normalizedType is "audio/mp4" or "audio/x-m4a",
            ".mp4" => normalizedType == "video/mp4",
            ".webm" => normalizedType is "video/webm" or "audio/webm",
            _ => false
        };
    }
}
