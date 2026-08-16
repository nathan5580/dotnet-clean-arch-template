using Databases.Core.Entities;
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

public sealed class UserActionAuditConfiguration : IEntityTypeConfiguration<UserActionAudit>
{
    public void Configure(EntityTypeBuilder<UserActionAudit> builder)
    {
        builder.ToTable("UserActionAudit", "Auth");

        builder.HasKey(e => e.AuditId)
            .HasName("PK-Auth_UserActionAudit_AuditId");

        builder.Property(e => e.Action).IsRequired().HasMaxLength(100);
        builder.Property(e => e.IpAddress).HasMaxLength(45);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .HasConstraintName("FK-Auth_UserActionAudit_UserId_UserId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX-Auth_UserActionAudit_UserId");

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX-Auth_UserActionAudit_Timestamp");
    }
}
