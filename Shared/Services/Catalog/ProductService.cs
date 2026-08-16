using Shared.Mapping.Catalog;
using Shared.Resources.HTTP.Catalog.GET;
using Shared.Resources.HTTP.Catalog.POST;
using Shared.Resources.HTTP.Catalog.PUT;

namespace Shared.Services.Catalog;

public interface IProductService
{
    Task<List<GetProduct>> GetProducts(CancellationToken ct);
    Task<GetProduct> GetProduct(Guid id, CancellationToken ct);
    Task<GetProduct> PostProduct(PostProductRequest request, CancellationToken ct);
    Task<GetProduct> PutProduct(Guid id, PutProductRequest request, CancellationToken ct);
    Task DeleteProduct(Guid id, CancellationToken ct);
}

public sealed class ProductService(
    AppDbContext db,
    IProductMapper mapper,
    ILogger<ProductService> log) : IProductService
{
    public async Task<List<GetProduct>> GetProducts(CancellationToken ct)
    {
        var products = await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return mapper.ToGetProducts(products);
    }

    public async Task<GetProduct> GetProduct(Guid id, CancellationToken ct)
    {
        var product = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == id, ct)
            .ConfigureAwait(false);

        if (product is null)
            throw new KeyNotFoundException($"Product '{id}' was not found.");

        return mapper.ToGetProduct(product);
    }

    public async Task<GetProduct> PostProduct(PostProductRequest request, CancellationToken ct)
    {
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        log.LogInformation("Created product {ProductId}", product.ProductId);

        return mapper.ToGetProduct(product);
    }

    public async Task<GetProduct> PutProduct(Guid id, PutProductRequest request, CancellationToken ct)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.ProductId == id, ct)
            .ConfigureAwait(false);

        if (product is null)
            throw new KeyNotFoundException($"Product '{id}' was not found.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Category = request.Category;
        product.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        log.LogInformation("Updated product {ProductId}", product.ProductId);

        return mapper.ToGetProduct(product);
    }

    public async Task DeleteProduct(Guid id, CancellationToken ct)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.ProductId == id, ct)
            .ConfigureAwait(false);

        if (product is null)
            throw new KeyNotFoundException($"Product '{id}' was not found.");

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        log.LogInformation("Deleted product {ProductId}", id);
    }
}
