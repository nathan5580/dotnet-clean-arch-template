namespace Web.Services;

public sealed class ThemeService
{
    public string PrimaryColor { get; private set; } = "#3b82f6";

    public void ApplyBrandColors(string primary)
    {
        PrimaryColor = primary;
        // Apply via JS interop
    }

    public void ResetToDefault()
    {
        PrimaryColor = "#3b82f6";
    }
}
