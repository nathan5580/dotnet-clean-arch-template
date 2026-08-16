namespace Web.Components.Surface;

public partial class MetaPanel : ComponentBase
{
    [Parameter] public string? Kicker { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter] public string? Error { get; set; }
    [Parameter] public RenderFragment? Body { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnRetry { get; set; }
    [Parameter] public string? CssClass { get; set; }
}
