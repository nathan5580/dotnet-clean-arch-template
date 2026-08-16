using Databases.Core;
using Microsoft.EntityFrameworkCore;
using Shared.Mapping.Catalog;
using Shared.Resources.Enums;
using Shared.Resources.HTTP.Catalog.POST;
using Shared.Services.Catalog;

namespace Shared.Tests;

public sealed class ProductServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    private static ProductService CreateService(AppDbContext db) =>
        new(db, new ProductMapper(), NullLogger<ProductService>.Instance);

    [Fact]
    public async Task PostProduct_WithValidRequest_PersistsAndReturnsProduct()
    {
        var db = CreateDbContext();
        var service = CreateService(db);

        var request = new PostProductRequest
        {
            Name = "Widget",
            Description = "A useful widget",
            Price = 19.99m,
            Category = ProductCategory.Electronics
        };

        var created = await service.PostProduct(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.ProductId);
        Assert.Equal("Widget", created.Name);
        Assert.Equal(ProductCategory.Electronics, created.Category);
        Assert.True(created.IsActive);
        Assert.Equal(1, await db.Products.CountAsync());
    }

    [Fact]
    public async Task GetProduct_WithExistingId_ReturnsProduct()
    {
        var db = CreateDbContext();
        var service = CreateService(db);

        var created = await service.PostProduct(
            new PostProductRequest { Name = "Shirt", Price = 9.50m, Category = ProductCategory.Apparel },
            CancellationToken.None);

        var fetched = await service.GetProduct(created.ProductId, CancellationToken.None);

        Assert.Equal(created.ProductId, fetched.ProductId);
        Assert.Equal("Shirt", fetched.Name);
        Assert.Equal(ProductCategory.Apparel, fetched.Category);
    }

    [Fact]
    public async Task GetProduct_WithMissingId_ThrowsKeyNotFoundException()
    {
        var db = CreateDbContext();
        var service = CreateService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetProduct(Guid.NewGuid(), CancellationToken.None));
    }
}
