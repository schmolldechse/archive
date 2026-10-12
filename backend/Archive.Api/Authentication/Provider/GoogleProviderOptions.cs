using System.ComponentModel.DataAnnotations;

namespace Archive.Api.Authentication.Provider;

public sealed class GoogleProviderOptions
{
    public const string SectionName = "Authentication:Google";

    [Required]
    public string ClientId { get; init; } = "";

    [Required]
    public string ClientSecret { get; init; } = "";
}
