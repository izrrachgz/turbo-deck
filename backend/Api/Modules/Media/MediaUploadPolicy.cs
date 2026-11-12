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
}
