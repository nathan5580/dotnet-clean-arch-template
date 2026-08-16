using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Databases.Auth;

public sealed class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("User", "Auth");

        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.IsActive).HasDefaultValue(true);
    }
}
