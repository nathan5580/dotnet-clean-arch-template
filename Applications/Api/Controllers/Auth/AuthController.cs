using System.Security.Claims;
using Shared.Resources.HTTP.Auth.GET;
using Shared.Resources.HTTP.Auth.POST;
using Shared.Resources.HTTP.Common;
using Shared.Services.Auth;

namespace Api.Controllers.Auth;

[ApiController]
[Tags(OpenApiTagNames.Auth)]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<PostAuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PostAuthResponse>>> PostRegister(
        [FromBody] PostAuthRegisterRequest request,
        CancellationToken ct)
    {
        var (user, token) = await authService.Register(request, ct);

        var me = await authService.GetMe(user.Id, ct);
        return CreatedAtAction(nameof(GetMe), ApiResponse<PostAuthResponse>.Created(new PostAuthResponse(me, token)));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<PostAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PostAuthResponse>>> PostLogin(
        [FromBody] PostAuthLoginRequest request,
        CancellationToken ct)
    {
        var (user, token) = await authService.Login(request, ct);

        var me = await authService.GetMe(user.Id, ct);
        return Ok(ApiResponse<PostAuthResponse>.Ok(new PostAuthResponse(me, token)));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetMe>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetMe>>> GetMe(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity not found.");

        var me = await authService.GetMe(userId, ct);
        return Ok(ApiResponse<GetMe>.Ok(me));
    }
}
