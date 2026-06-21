namespace Bluewater.Core.Services.Imaging;

/// <summary>
/// Reads width/height directly from a PNG header rather than pulling in a full image library,
/// which avoids the licensing requirements that the common .NET imaging packages
/// (e.g. SixLabors.ImageSharp v3+) impose for commercial use.
/// </summary>
public static class PngDimensionReader
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool TryReadDimensions(byte[] bytes, out (int Width, int Height) size)
    {
        size = default;

        if (bytes.Length < 24 || !bytes.AsSpan(0, 8).SequenceEqual(PngSignature))
        {
            return false;
        }

        // IHDR is always the first chunk: 4-byte length, 4-byte "IHDR" type, then 4-byte width + 4-byte height, big-endian.
        size = (ReadUInt32BigEndian(bytes, 16), ReadUInt32BigEndian(bytes, 20));
        return true;
    }

    private static int ReadUInt32BigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
}
