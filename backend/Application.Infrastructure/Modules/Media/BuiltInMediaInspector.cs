using System.Text.Json;
using System.Buffers.Binary;
using Application.Modules.Media;

namespace Application.Infrastructure.Modules.Media;

internal sealed class BuiltInMediaInspector : IMediaInspector
{
    private const int HeaderSize = 64 * 1024;

    public async Task<MediaInspectionResult> InspectAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[HeaderSize];
        int read = await ReadHeaderAsync(content, header, cancellationToken);
        ReadOnlyMemory<byte> bytes = header.AsMemory(0, read);
        string kind = GetKind(contentType);

        (int? width, int? height) = GetDimensions(bytes.Span, fileName);
        WavInspection? wav = kind == "audio" && Path.GetExtension(fileName).Equals(".wav", StringComparison.OrdinalIgnoreCase)
            ? await InspectWavAsync(content, bytes, cancellationToken)
            : null;

        return new MediaInspectionResult(
            kind,
            wav is not null || width is not null ? "inspected" : "basic",
            width,
            height,
            wav?.DurationMilliseconds,
            null,
            wav?.Codec,
            kind == "audio",
            wav?.WaveformData);
    }

    private static async Task<int> ReadHeaderAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken);
            if (read == 0)
                break;
            total += read;
        }
        return total;
    }

    private static string GetKind(string contentType) => contentType.Split(';', 2)[0].Trim().ToLowerInvariant() switch
    {
        string value when value.StartsWith("image/", StringComparison.Ordinal) => "image",
        string value when value.StartsWith("audio/", StringComparison.Ordinal) => "audio",
        string value when value.StartsWith("video/", StringComparison.Ordinal) => "video",
        _ => "unknown"
    };

    private static (int? Width, int? Height) GetDimensions(ReadOnlySpan<byte> bytes, string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".png" && bytes.Length >= 24)
            return (BinaryPrimitives.ReadInt32BigEndian(bytes[16..20]), BinaryPrimitives.ReadInt32BigEndian(bytes[20..24]));
        if (extension == ".gif" && bytes.Length >= 10)
            return (BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..8]), BinaryPrimitives.ReadUInt16LittleEndian(bytes[8..10]));
        if (extension == ".webp" && bytes.Length >= 30 && bytes[12..16].SequenceEqual("VP8X"u8))
            return (1 + ReadUInt24(bytes[24..27]), 1 + ReadUInt24(bytes[27..30]));
        if (extension is ".jpg" or ".jpeg")
            return GetJpegDimensions(bytes);
        return (null, null);
    }

    private static (int? Width, int? Height) GetJpegDimensions(ReadOnlySpan<byte> bytes)
    {
        int offset = 2;
        while (offset + 8 < bytes.Length)
        {
            if (bytes[offset] != 0xFF)
            {
                offset++;
                continue;
            }
            byte marker = bytes[offset + 1];
            if (marker is >= 0xC0 and <= 0xC3 or >= 0xC5 and <= 0xC7 or >= 0xC9 and <= 0xCB or >= 0xCD and <= 0xCF)
                return (BinaryPrimitives.ReadUInt16BigEndian(bytes[(offset + 7)..(offset + 9)]),
                    BinaryPrimitives.ReadUInt16BigEndian(bytes[(offset + 5)..(offset + 7)]));
            int length = BinaryPrimitives.ReadUInt16BigEndian(bytes[(offset + 2)..(offset + 4)]);
            if (length < 2)
                break;
            offset += length + 2;
        }
        return (null, null);
    }

    private static int ReadUInt24(ReadOnlySpan<byte> bytes) => bytes[0] | bytes[1] << 8 | bytes[2] << 16;

    private static async Task<WavInspection?> InspectWavAsync(
        Stream stream,
        ReadOnlyMemory<byte> initial,
        CancellationToken cancellationToken)
    {
        if (initial.Length < 44 || !initial.Span[..4].SequenceEqual("RIFF"u8) || !initial.Span[8..12].SequenceEqual("WAVE"u8))
            return null;

        ReadOnlySpan<byte> bytes = initial.Span;
        int byteRate = BinaryPrimitives.ReadInt32LittleEndian(bytes[28..32]);
        int dataLength = BinaryPrimitives.ReadInt32LittleEndian(bytes[40..44]);
        if (byteRate <= 0 || dataLength <= 0)
            return null;

        long duration = (long)Math.Round(dataLength * 1000d / byteRate);
        string? waveform = null;
        if (stream.CanSeek && stream.Length <= 25L * 1024 * 1024)
        {
            stream.Position = 44;
            byte[] data = new byte[Math.Min(dataLength, (int)Math.Min(int.MaxValue, stream.Length - 44))];
            await stream.ReadExactlyAsync(data, cancellationToken);
            waveform = JsonSerializer.Serialize(BuildWaveform(data, 256));
        }
        return new WavInspection(duration, "pcm", waveform);
    }

    private static decimal[] BuildWaveform(byte[] data, int bucketCount)
    {
        if (data.Length < 2)
            return [];
        int sampleCount = data.Length / 2;
        int actualBuckets = Math.Min(bucketCount, sampleCount);
        decimal[] peaks = new decimal[actualBuckets];
        for (int bucket = 0; bucket < actualBuckets; bucket++)
        {
            int start = bucket * sampleCount / actualBuckets;
            int end = (bucket + 1) * sampleCount / actualBuckets;
            int peak = 0;
            for (int sample = start; sample < end; sample++)
                peak = Math.Max(peak, Math.Abs((int)BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(sample * 2, 2))));
            peaks[bucket] = Math.Round(peak / 32768m, 4);
        }
        return peaks;
    }

    private sealed record WavInspection(long DurationMilliseconds, string Codec, string? WaveformData);
}
