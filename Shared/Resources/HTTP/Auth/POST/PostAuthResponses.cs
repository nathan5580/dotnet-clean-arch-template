using Shared.Resources.HTTP.Auth.GET;

namespace Shared.Resources.HTTP.Auth.POST;

public record PostAuthResponse(GetMe User, string Token);
