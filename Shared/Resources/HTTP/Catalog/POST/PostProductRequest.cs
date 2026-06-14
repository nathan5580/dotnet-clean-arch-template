using Shared.Resources.Enums;

namespace Shared.Resources.HTTP.Catalog.POST;

public record PostProductRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public required ProductCategory Category { get; init; }
}
