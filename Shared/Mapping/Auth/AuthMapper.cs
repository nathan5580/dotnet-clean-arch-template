using AutoMapper;
using Databases.Core.Entities;
using Shared.Resources.HTTP.Auth.GET;

namespace Shared.Mapping.Auth;

public interface IAuthMapper
{
    GetMe ToGetMe(ApplicationUser user);
}

public sealed class AuthMapper(IMapper mapper) : IAuthMapper
{
    public GetMe ToGetMe(ApplicationUser user)
        => mapper.Map<GetMe>(user);
}

public sealed class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<ApplicationUser, GetMe>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Roles, o => o.Ignore());
    }
}
