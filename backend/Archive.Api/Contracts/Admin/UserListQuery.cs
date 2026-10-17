using System.ComponentModel;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Archive.Core.Entities.Authentication.Permissible;

namespace Archive.Api.Contracts.Admin;

/// <summary>Bounded registered-user search; role filtering combines with free text.</summary>
[Description("Bounded registered-user search by handle, display name, and role.")]
public sealed class UserListQuery
{
    [StringLength(128)]
    [JsonPropertyName("q")]
    [Description("Case-insensitive search across handle, display name, and role. Combines with the role filter.")]
    [FromQuery(Name = "q")]
    public string? Q { get; init; }

    [EnumDataType(typeof(ArchiveRole))]
    [JsonPropertyName("role")]
    [Description("Optional role filter. Only USER and ADMIN are accepted.")]
    [FromQuery(Name = "role")]
    public ArchiveRole? Role { get; init; }

    [Range(1, 10000)]
    [JsonPropertyName("page")]
    [Description("One-based page number. Defaults to 1.")]
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    [JsonPropertyName("pageSize")]
    [Description("Maximum profiles per page, from 1 to 100. Defaults to 25.")]
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = 25;
}
