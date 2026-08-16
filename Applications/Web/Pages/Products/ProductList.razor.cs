using Microsoft.AspNetCore.Components;
using Shared.Resources.HTTP.Catalog.GET;
using Web.Services;

namespace Web.Pages.Products;

public partial class ProductList : ComponentBase
{
    [Inject] private IApiClient ApiClient { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private LocalizationService Localization { get; set; } = default!;

    private List<GetProduct> _products = [];
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
            var response = await ApiClient.GetAsync<List<GetProduct>>("api/products");
            _products = response.Data ?? [];
        }
        catch
        {
            _error = T("list.error");
        }
        finally
        {
            _loading = false;
        }
    }

    private void GoToCreate() => Navigation.NavigateTo("/products/new");

    private void GoToDetail(Guid id) => Navigation.NavigateTo($"/products/{id}");

    private string T(string key) => Localization.T($"products.{key}");
}
