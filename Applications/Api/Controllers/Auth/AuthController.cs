using Api.Authorization;
using Databases.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Shared.Resources.HTTP.Auth.GET;
using Shared.Resources.HTTP.Auth.POST;
using Shared.Resources.HTTP.Common;
using Shared.Mapping.Auth;
using Shared.Services.Auth;

namespace Api.Controllers.Auth;

[ApiController]
[ApiVersion("1.0")]
[Tags(OpenApiTagNames.Auth)]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService, IAuthMapper authMapper, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<GetMe>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GetMe>>> PostRegister(
        [FromBody] PostAuthRegisterRequest request,
        CancellationToken ct)
    {
        var (user, token) = await authService.Register(request, ct);
        var me = authMapper.ToGetMe(user);
        return CreatedAtAction(nameof(GetMe), ApiResponse<GetMe>.Created(me, token));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<GetMe>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetMe>>> PostLogin(
        [FromBody] PostAuthLoginRequest request,
        CancellationToken ct)
    {
        var (user, token) = await authService.Login(request, ct);
        var me = authMapper.ToGetMe(user);
        return Ok(ApiResponse<GetMe>.Success(me, token));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetMe>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetMe>>> GetMe(CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            throw new UnauthorizedAccessException("User not found.");

        var me = authMapper.ToGetMe(user);
        return Ok(ApiResponse<GetMe>.Success(me));
    }
}
