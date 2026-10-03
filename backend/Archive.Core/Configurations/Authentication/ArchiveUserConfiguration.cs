using Archive.Core.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archive.Core.Configurations.Authentication;

public sealed class ArchiveUserConfiguration : IEntityTypeConfiguration<ArchiveUser>
{
    public void Configure(EntityTypeBuilder<ArchiveUser> builder)
    {
        builder.HasIndex(user => user.Handle)
            .IsUnique()
            .HasFilter("handle <> ''");
    }
}
