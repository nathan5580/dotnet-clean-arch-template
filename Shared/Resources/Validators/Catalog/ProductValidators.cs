using FluentValidation;
using Shared.Resources.HTTP.Catalog.POST;
using Shared.Resources.HTTP.Catalog.PUT;

namespace Shared.Resources.Validators.Catalog;

/// <summary>
/// Allowed values for a product's category at the HTTP boundary. Mirrors
/// <c>Databases.Core.Enums.ProductCategory</c>; the canonical enum lives in the
/// Databases layer, which Shared.Resources must not depend on (it would invert the
/// layering — Databases.Core already references Shared.Resources).
/// </summary>
internal static class ProductCategoryNames
{
    private static readonly HashSet<string> Set =
        new(["General", "Electronics", "Apparel", "Food"], StringComparer.OrdinalIgnoreCase);

    public static string Allowed => string.Join(", ", Set);

    public static bool IsValid(string? value) => value is not null && Set.Contains(value);
}

public sealed class PostProductRequestValidator : AbstractValidator<PostProductRequest>
{
    public PostProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).NotEmpty()
            .Must(ProductCategoryNames.IsValid)
            .WithMessage($"Category must be one of: {ProductCategoryNames.Allowed}.");
    }
}

public sealed class PutProductRequestValidator : AbstractValidator<PutProductRequest>
{
    public PutProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).NotEmpty()
            .Must(ProductCategoryNames.IsValid)
            .WithMessage($"Category must be one of: {ProductCategoryNames.Allowed}.");
    }
}
