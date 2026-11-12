namespace Application.Modules.Media;

public static class CellThumbnailSpecifications
{
    public const int OutputSize = 320;
    public const int DecodeTargetSize = 1280;
    public const long MaximumSourceBytes = 25L * 1024 * 1024;
    public const long MaximumOutputBytes = 2L * 1024 * 1024;
}
