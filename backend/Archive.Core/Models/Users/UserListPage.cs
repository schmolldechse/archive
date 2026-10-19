namespace Archive.Core.Models.Users;

/// <summary>A stable handle-ordered page of registered accounts.</summary>
public sealed record UserListPage(IReadOnlyList<UserListEntry> Items, int Total, int Page, int PageSize);
