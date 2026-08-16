namespace Web.Components.Layout;

public partial class AppPageFrame : ComponentBase
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public string? Width { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string GetWidthClass() => Width switch
    {
        "Narrow" => "max-w-lg",
        "Medium" => "max-w-2xl",
        _ => "max-w-7xl"
    };
}
