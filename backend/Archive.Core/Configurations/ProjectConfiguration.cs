using Archive.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Archive.Core.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder) =>
        builder.Property(project => project.ProjectKeyHash)
            .HasComputedColumnSql("digest(project_key, 'sha256')", stored: true);
}
