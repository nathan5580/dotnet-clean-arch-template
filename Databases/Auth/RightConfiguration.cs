using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Databases.Auth;

public sealed class RightConfiguration : IEntityTypeConfiguration<Right>
{
    public void Configure(EntityTypeBuilder<Right> builder)
    {
        builder.ToTable("Right", "Auth");

        builder.HasKey(right => right.RightId)
            .HasName("PK-Auth_Right_RightId");

        builder.Property(right => right.Code).IsRequired().HasMaxLength(200);
        builder.Property(right => right.Description).IsRequired().HasMaxLength(500);
        builder.HasIndex(right => right.Code)
            .IsUnique()
            .HasDatabaseName("IX-Auth_Right_Code");
    }
}
