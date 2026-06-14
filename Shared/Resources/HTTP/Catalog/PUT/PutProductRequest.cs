namespace Shared.Resources.HTTP.Catalog.PUT;

public record PutProductRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public required string Category { get; init; }
    public bool IsActive { get; init; }
}
