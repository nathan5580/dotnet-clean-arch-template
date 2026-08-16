using Microsoft.AspNetCore.Components;
using Shared.Resources.Enums;
using Shared.Resources.HTTP.Catalog.GET;
using Shared.Resources.HTTP.Catalog.POST;
using Web.Services;

namespace Web.Pages.Products;

public partial class ProductCreate : ComponentBase
{
    [Inject] private IApiClient ApiClient { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private LocalizationService Localization { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    private string _name = string.Empty;
    private string? _description;
    private decimal _price;
    private ProductCategory _category = ProductCategory.General;
    private bool _loading = true;
    private bool _submitting;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await Localization.LoadNamespace("products");

        _loading = false;
    }

    private async Task Submit()
    {
        _submitting = true;
        _error = null;

        try
        {
            var request = new PostProductRequest
            {
                Name = _name,
                Description = string.IsNullOrWhiteSpace(_description) ? null : _description,
                Price = _price,
                Category = _category
            };

            var response = await ApiClient.PostAsync<GetProduct>("api/products", request);
            if (response.Data is null)
            {
                _error = T("create.error");
                return;
            }

            Toast.ShowSuccess(T("create.success"));
            Navigation.NavigateTo($"/products/{response.Data.ProductId}");
        }
        catch
        {
            _error = T("create.error");
        }
        finally
        {
            _submitting = false;
        }
    }

    private void Cancel() => Navigation.NavigateTo("/products");

    private string T(string key) => Localization.T($"products.{key}");
}
