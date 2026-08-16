using Shared.Resources.HTTP.Catalog.POST;
using Shared.Resources.HTTP.Catalog.PUT;

namespace Shared.Resources.Validators.Catalog;

public sealed class PostProductRequestValidator : AbstractValidator<PostProductRequest>
{
    public PostProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).IsInEnum();
    }
}

public sealed class PutProductRequestValidator : AbstractValidator<PutProductRequest>
{
    public PutProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).IsInEnum();
    }
}
