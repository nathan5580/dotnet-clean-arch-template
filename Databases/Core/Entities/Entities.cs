using Microsoft.AspNetCore.Identity;

namespace Databases.Core.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public Guid Id
    {
        get => Guid.Parse(base.Id);
        set => base.Id = value.ToString();
    }
}

public sealed class ApplicationRole : IdentityRole
{
    [NotMapped]
    public Guid Id
    {
        get => Guid.Parse(base.Id);
        set => base.Id = value.ToString();
    }
}

public sealed class UserActionAudit
{
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
