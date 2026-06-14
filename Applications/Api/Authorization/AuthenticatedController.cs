using System.Security.Claims;

namespace Api.Authorization;

[Authorize]
public abstract class AuthenticatedController : ControllerBase
{
    protected string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");
}
