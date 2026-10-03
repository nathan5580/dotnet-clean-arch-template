using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Shared.Resources.Enums;

namespace Databases.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ApplicationRole : IdentityRole
{
}

public class Product
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public ProductCategory Category { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public Guid Id { get => ProductId; set => ProductId = value; }
}

public class Right
{
    public Guid RightId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

[PrimaryKey(nameof(RoleId), nameof(RightId))]
public class RoleRight
{
    public string RoleId { get; set; } = string.Empty;
    public Guid RightId { get; set; }
    public ApplicationRole Role { get; set; } = null!;
    public Right Right { get; set; } = null!;
}
