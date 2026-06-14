using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Shared.Resources.Enums;

namespace Databases.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public new Guid Id
    {
        get => Guid.Parse(base.Id);
        set => base.Id = value.ToString();
    }
}

public class ApplicationRole : IdentityRole
{
    [NotMapped]
    public new Guid Id
    {
        get => Guid.Parse(base.Id);
        set => base.Id = value.ToString();
    }
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

public class UserActionAudit
{
    [Key]
    public Guid AuditId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public Guid Id { get => AuditId; set => AuditId = value; }

    public ApplicationUser User { get; set; } = null!;
}
