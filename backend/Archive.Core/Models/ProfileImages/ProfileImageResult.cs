namespace Archive.Core.Models.ProfileImages;

public abstract record ProfileImageResult
{
    private ProfileImageResult() { }
    public sealed record Found(ProfileImage Image) : ProfileImageResult;
    public sealed record UserNotFound : ProfileImageResult;
    public sealed record Unavailable : ProfileImageResult;
}
