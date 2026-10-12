namespace Archive.Core.Models.Users;

public sealed record ArchiveCreator(string Handle, string DisplayName, byte[]? profileImage);
