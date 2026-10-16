using SkiaSharp;

namespace Archive.Core.Models.ProfileImages;

public static class ProfileImageEncoding
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public const int MaxSourceDimension = 4096;
    public const int MaxDimension = 512;

    public static byte[]? Encode(byte[]? content, out string? error)
    {
        error = null;
        if (content is not { Length: > 0 })
        {
            error = "No image data.";
            return null;
        }
        if (content.Length > MaxBytes)
        {
            error = "Image exceeds 2 MiB.";
            return null;
        }

        using var stream = new MemoryStream(content, writable: false);
        using var codec = SKCodec.Create(stream);
        if (codec is null || codec.EncodedFormat is not
            (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
        {
            error = "Image must be a readable JPEG, PNG or WebP.";
            return null;
        }

        var info = codec.Info;
        if (info.Width is <= 0 or > MaxSourceDimension || info.Height is <= 0 or > MaxSourceDimension)
        {
            error = "Image dimensions must be between 1 and 4096 pixels per side.";
            return null;
        }

        using var colorSpace = SKColorSpace.CreateSrgb();
        using var bitmap = new SKBitmap(new SKImageInfo(info.Width, info.Height,
            SKColorType.Rgba8888, SKAlphaType.Unpremul, colorSpace));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
        {
            error = "Image could not be fully decoded.";
            return null;
        }

        var origin = codec.EncodedOrigin;
        var swapsDimensions = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or
            SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = swapsDimensions ? info.Height : info.Width;
        var height = swapsDimensions ? info.Width : info.Height;
        var scale = Math.Min(1d, (double)MaxDimension / Math.Max(width, height));
        var outputWidth = Math.Max(1, (int)Math.Round(width * scale));
        var outputHeight = Math.Max(1, (int)Math.Round(height * scale));

        if (origin == SKEncodedOrigin.TopLeft && outputWidth == info.Width && outputHeight == info.Height)
            return EncodeBitmap(bitmap, out error);

        using var output = new SKBitmap(new SKImageInfo(outputWidth, outputHeight,
            SKColorType.Rgba8888, SKAlphaType.Premul, colorSpace));
        using (var canvas = new SKCanvas(output))
        using (var image = SKImage.FromBitmap(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale((float)outputWidth / width, (float)outputHeight / height);
            canvas.Concat(Orientation(origin, info.Width, info.Height));
            canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        }

        return EncodeBitmap(output, out error);
    }

    private static byte[]? EncodeBitmap(SKBitmap bitmap, out string? error)
    {
        error = null;
        using var pixels = bitmap.PeekPixels();
        // In lossless mode, quality controls compression effort.
        using var encoded = pixels.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 75))
            ?? throw new InvalidOperationException("The profile image could not be encoded as WebP.");
        if (encoded.Size > MaxBytes)
        {
            error = "Encoded image exceeds 2 MiB.";
            return null;
        }
        return encoded.ToArray();
    }

    private static SKMatrix Orientation(SKEncodedOrigin origin, int width, int height) => origin switch
    {
        SKEncodedOrigin.TopRight => new(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity
    };
}
