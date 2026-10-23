using Archive.Core.Entities.Authentication;
using Archive.Core.Models.Users;
using Riok.Mapperly.Abstractions;

namespace Archive.Core.Mapping;

/// <summary>Maps authentication entities to bounded Core user projections.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class UserModelMapper
{
    [MapPropertyFromSource(nameof(PublicUser.HasProfileImage), Use = nameof(HasProfileImage))]
    internal static partial PublicUser ToPublicUser(ArchiveUser user);

    [MapPropertyFromSource(nameof(UserListEntry.HasProfileImage), Use = nameof(HasProfileImage))]
    private static partial UserListEntry ToListEntry(ArchiveUser user);

    internal static partial IQueryable<UserListEntry> ProjectToListEntries(this IQueryable<ArchiveUser> users);

    [MapPropertyFromSource(nameof(UserProfileEntry.Profile), Use = nameof(ToPublicUser))]
    private static partial UserProfileEntry ToProfileEntry(ArchiveUser user);

    internal static partial IQueryable<UserProfileEntry> ProjectToProfileEntries(this IQueryable<ArchiveUser> users);

    internal static UserListPage ToPage(IReadOnlyList<UserListEntry> items, int total, int page, int pageSize) =>
        MapPage(new UserListPageMapping(items, total, page, pageSize));

    private static partial UserListPage MapPage(UserListPageMapping source);

    private static bool HasProfileImage(ArchiveUser user) =>
        user.ProfileImage != null && user.ProfileImage.Length > 0;
}

/// <summary>Pairs an internal identifier with an API-safe profile during history resolution.</summary>
internal sealed record UserProfileEntry(Guid Id, PublicUser Profile);

/// <summary>Bundles pagination values so Mapperly can invoke the immutable page constructor.</summary>
internal sealed record UserListPageMapping(IReadOnlyList<UserListEntry> Items, int Total, int Page, int PageSize);
