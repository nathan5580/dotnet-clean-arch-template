using FluentValidation;
using Shared.Resources.HTTP.Auth.POST;

namespace Shared.Resources.Validators.Auth;

public sealed class PostAuthRegisterRequestValidator : AbstractValidator<PostAuthRegisterRequest>
{
    public PostAuthRegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password)
            .WithMessage("Passwords do not match.");
    }
}

public sealed class PostAuthLoginRequestValidator : AbstractValidator<PostAuthLoginRequest>
{
    public PostAuthLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
