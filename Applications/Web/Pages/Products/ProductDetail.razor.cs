using Shared.Resources.HTTP.Catalog.GET;

namespace Web.Pages.Products;

public partial class ProductDetail : ComponentBase
{
    [Parameter] public Guid ProductId { get; set; }

    [Inject] private IApiClient ApiClient { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private LocalizationService Localization { get; set; } = default!;

    private GetProduct? _product;
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await Localization.LoadNamespace("products");

        await Load();
    }

    private async Task Load()
    {
        _loading = true;
        _error = null;

        try
        {
            var response = await ApiClient.GetAsync<GetProduct>($"api/products/{ProductId}");
            _product = response.Data;
            if (_product is null)
                _error = T("detail.error");
        }
        catch
        {
            _error = T("detail.error");
        }
        finally
        {
            _loading = false;
        }
    }

    private void GoBack() => Navigation.NavigateTo("/products");

    private string T(string key) => Localization.T($"products.{key}");
}
