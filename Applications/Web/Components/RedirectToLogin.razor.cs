namespace Web.Components;

public partial class RedirectToLogin : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/login");
    }
}
