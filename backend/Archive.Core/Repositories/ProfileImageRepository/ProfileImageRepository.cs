using Archive.Core.Models.ProfileImages;
using Archive.Core.Repositories.UserRepository;
using SkiaSharp;
using System.Security.Cryptography;

namespace Archive.Core.Repositories.ProfileImageRepository;

internal sealed class ProfileImageRepository(IUserRepository userRepository) : IProfileImageRepository
{
    public async Task<ProfileImageResult> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is { IsRegistrationComplete: true }
            ? Encode(user.ProfileImage)
            : new ProfileImageResult.UserNotFound();
    }

    public async Task<ProfileImageResult> GetByHandleAsync(string handle, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByHandleAsync(handle, cancellationToken);
        return user is { IsRegistrationComplete: true }
            ? Encode(user.ProfileImage)
            : new ProfileImageResult.UserNotFound();
    }

    private static ProfileImageResult Encode(byte[]? content)
    {
        using var source = new MemoryStream(content ?? [], writable: false);
        using var bitmap = SKBitmap.Decode(source);
        if (bitmap is null) return new ProfileImageResult.Unavailable();

        using var pixels = bitmap.PeekPixels();
        using var encoded = pixels.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100))
            ?? throw new InvalidOperationException("The profile image could not be encoded as WebP.");

        var bytes = encoded.ToArray();
        return new ProfileImageResult.Found(new(bytes, Convert.ToHexStringLower(SHA256.HashData(bytes))));
    }
}
