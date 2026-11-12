using SkiaSharp;
using Application.Modules.Media;

namespace Application.Infrastructure.Modules.Media;

internal sealed class CellThumbnailProcessor : ICellThumbnailProcessor
{
    private const long MaximumDecodedPixelCount = 64L * 1024 * 1024;

    public async Task<byte[]> CreatePngAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        await using var encoded = new MemoryStream();
        byte[] buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) != 0)
        {
            total += read;
            if (total > CellThumbnailSpecifications.MaximumSourceBytes)
                throw new InvalidDataException("The uploaded thumbnail exceeds the supported source size.");
            await encoded.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        encoded.Position = 0;
        using SKCodec codec = SKCodec.Create(encoded)
            ?? throw new InvalidDataException("The uploaded thumbnail is not a valid supported image.");
        SKImageInfo sourceInfo = codec.Info;
        if (sourceInfo.Width < 1 || sourceInfo.Height < 1 || (long)sourceInfo.Width * sourceInfo.Height > MaximumDecodedPixelCount)
            throw new InvalidDataException("The uploaded thumbnail dimensions are not supported.");

        float decodeScale = Math.Min(1f,
            CellThumbnailSpecifications.DecodeTargetSize / (float)Math.Max(sourceInfo.Width, sourceInfo.Height));
        SKSizeI decodedSize = codec.GetScaledDimensions(decodeScale);
        var decodedInfo = new SKImageInfo(
            decodedSize.Width,
            decodedSize.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul,
            SKColorSpace.CreateSrgb());
        using SKBitmap decoded = SKBitmap.Decode(codec, decodedInfo)
            ?? throw new InvalidDataException("The uploaded thumbnail could not be decoded.");

        int outputSize = CellThumbnailSpecifications.OutputSize;
        using var thumbnail = new SKBitmap(new SKImageInfo(
            outputSize,
            outputSize,
            SKColorType.Rgba8888,
            SKAlphaType.Premul,
            SKColorSpace.CreateSrgb()));
        using (var canvas = new SKCanvas(thumbnail))
        using (var paint = new SKPaint { IsAntialias = true })
        {
            canvas.Clear(SKColors.Transparent);
            float scale = Math.Max(outputSize / (float)decoded.Width, outputSize / (float)decoded.Height);
            float width = decoded.Width * scale;
            float height = decoded.Height * scale;
            var destination = SKRect.Create((outputSize - width) / 2f, (outputSize - height) / 2f, width, height);
            canvas.DrawBitmap(decoded, destination, new SKSamplingOptions(SKCubicResampler.Mitchell), paint);
            canvas.Flush();
        }

        using SKPixmap pixels = thumbnail.PeekPixels()
            ?? throw new InvalidDataException("The optimized thumbnail pixels are unavailable.");
        using SKData png = pixels.Encode(new SKPngEncoderOptions(SKPngEncoderFilterFlags.AllFilters, 9))
            ?? throw new InvalidDataException("The optimized thumbnail could not be encoded.");
        return png.ToArray();
    }
}
