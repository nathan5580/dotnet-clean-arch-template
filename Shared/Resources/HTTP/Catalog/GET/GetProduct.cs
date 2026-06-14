namespace Shared.Resources.HTTP.Catalog.GET;

public record GetProduct
{
    public required Guid ProductId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public required string Category { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
