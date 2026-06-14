using Shared.Resources.HTTP.Catalog.GET;

namespace Shared.Mapping.Catalog;

public interface IProductMapper
{
    GetProduct ToGetProduct(Product product);
    List<GetProduct> ToGetProducts(IEnumerable<Product> products);
}

[Mapper]
public sealed partial class ProductMapper : IProductMapper
{
    public partial GetProduct ToGetProduct(Product product);

    public partial List<GetProduct> ToGetProducts(IEnumerable<Product> products);
}
