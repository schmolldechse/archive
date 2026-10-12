using System.ComponentModel.DataAnnotations;
using Npgsql;

namespace Archive.Core.Options;

public sealed class ConnectionStringOptions : IValidatableObject
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string ArchiveDatabase { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        bool valid;
        try
        {
            valid = !string.IsNullOrWhiteSpace(new NpgsqlConnectionStringBuilder(ArchiveDatabase).Host);
        }
        catch (ArgumentException)
        {
            valid = false;
        }
        if (!valid)
            yield return new ValidationResult("ArchiveDatabase must be a valid PostgreSQL connection string with a host.", [nameof(ArchiveDatabase)]);
    }
}
