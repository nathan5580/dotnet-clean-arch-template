using Databases.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Databases.Catalog;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {

        builder.ToTable("Product", "Catalog");

        builder.HasKey(e => e.ProductId)
            .HasName("PK-Catalog_Product_ProductId");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Price).HasPrecision(18, 2);

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX-Catalog_Product_Category");

    }
}
