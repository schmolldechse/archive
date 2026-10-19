using System.ComponentModel;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Archive.Api.Contracts.Authentication;

namespace Archive.Api.Contracts.Admin;

/// <summary>A handle-ordered page of fully registered user profiles.</summary>
[Description("A handle-ordered page of fully registered user profiles.")]
public sealed class UserListResponse
{
    [JsonPropertyName("items")]
    [Description("Registered user profiles using the shared profile contract, including public profile image URLs.")]
    public required IReadOnlyList<UserProfileResponse> Items { get; init; }

    [JsonPropertyName("total")]
    [Description("Total number of registered users matching the search and role filter.")]
    [Range(0, int.MaxValue)]
    public required int Total { get; init; }

    [JsonPropertyName("page")]
    [Description("Requested one-based page number.")]
    [Range(1, 10000)]
    public required int Page { get; init; }

    [JsonPropertyName("pageSize")]
    [Description("Maximum number of profiles returned in this page.")]
    [Range(1, 100)]
    public required int PageSize { get; init; }
}
