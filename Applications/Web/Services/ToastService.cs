namespace Web.Services;

public interface IToastService
{
    void ShowSuccess(string message);
    void ShowError(string message);
}

public sealed class ToastService : IToastService
{
    public event Action<string>? OnSuccessShown;
    public event Action<string>? OnErrorShown;

    public void ShowSuccess(string message) => OnSuccessShown?.Invoke(message);

    public void ShowError(string message) => OnErrorShown?.Invoke(message);
}
