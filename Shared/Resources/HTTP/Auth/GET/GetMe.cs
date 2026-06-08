namespace Shared.Resources.HTTP.Auth.GET;

public record GetMe
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public List<string> Roles { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
}

public record GetUserRole
{
    public required string RoleId { get; init; }
    public required string Name { get; init; }
}
