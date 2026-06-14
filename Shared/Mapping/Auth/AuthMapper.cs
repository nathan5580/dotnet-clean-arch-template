namespace Shared.Mapping.Auth;

public interface IAuthMapper
{
    GetMe ToGetMe(ApplicationUser user);
}

[Mapper]
public sealed partial class AuthMapper : IAuthMapper
{
    [MapProperty(nameof(ApplicationUser.Id), nameof(GetMe.UserId))]
    [MapperIgnoreTarget(nameof(GetMe.Roles))]
    public partial GetMe ToGetMe(ApplicationUser user);
}
