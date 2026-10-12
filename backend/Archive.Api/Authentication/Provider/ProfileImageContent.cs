namespace Archive.Api.Authentication.Provider;

internal static class ProfileImageContent
{
    public static string? ContentType(ReadOnlySpan<byte> image)
    {
        if (image.StartsWith(new byte[] { 0xff, 0xd8, 0xff })) return "image/jpeg";
        if (image.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })) return "image/png";
        if (image.Length >= 12 && image[..4].SequenceEqual("RIFF"u8) && image.Slice(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";
        return null;
    }
}
