namespace Shared.Resources.HTTP.Auth.POST;

public record PostAuthRegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string ConfirmPassword { get; init; }
}

public record PostAuthLoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
