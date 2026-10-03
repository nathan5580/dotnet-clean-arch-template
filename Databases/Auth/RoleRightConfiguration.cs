using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Databases.Auth;

public sealed class RoleRightConfiguration : IEntityTypeConfiguration<RoleRight>
{
    public void Configure(EntityTypeBuilder<RoleRight> builder)
    {
        builder.ToTable("RoleRight", "Auth");

        builder.HasKey(roleRight => new { roleRight.RoleId, roleRight.RightId })
            .HasName("PK-Auth_RoleRight_RoleId_RightId");

        builder.HasOne(roleRight => roleRight.Role)
            .WithMany()
            .HasForeignKey(roleRight => roleRight.RoleId)
            .HasConstraintName("FK-Auth_RoleRight_Role_ApplicationRole");

        builder.HasOne(roleRight => roleRight.Right)
            .WithMany()
            .HasForeignKey(roleRight => roleRight.RightId)
            .HasConstraintName("FK-Auth_RoleRight_Right_Right");
    }
}
