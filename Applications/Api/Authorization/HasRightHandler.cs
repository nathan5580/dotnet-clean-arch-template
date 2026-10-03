using System.Security.Claims;

namespace Api.Authorization;

public sealed record HasRightRequirement(string RightCode) : IAuthorizationRequirement;

public sealed class HasRightHandler(AppDbContext dbContext) : AuthorizationHandler<HasRightRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, HasRightRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return;

        var hasRight = await (
            from userRole in dbContext.UserRoles
            join roleRight in dbContext.RoleRights on userRole.RoleId equals roleRight.RoleId
            join right in dbContext.Rights on roleRight.RightId equals right.RightId
            where userRole.UserId == userId && right.Code == requirement.RightCode
            select right.RightId
        ).AnyAsync(context.Resource is HttpContext httpContext ? httpContext.RequestAborted : CancellationToken.None);

        if (hasRight)
            context.Succeed(requirement);
    }
}
