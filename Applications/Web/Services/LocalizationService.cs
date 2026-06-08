using System.Text.Json;
using Microsoft.JSInterop;

namespace Web.Services;

public sealed class LocalizationService
{
    private readonly Dictionary<string, string> _keys = [];
    private string _currentLang = "en";
    private Dictionary<string, string>? _fallbackDict;

    public string CurrentLanguage => _currentLang;

    public async Task InitializeAsync(string lang = "en")
    {
        _currentLang = lang;
        await LoadNamespace("common");
        _fallbackDict = new Dictionary<string, string>(_keys);
    }

    public async Task LoadNamespace(string ns)
    {
        try
        {
            using var http = new HttpClient();
            var response = await http.GetStringAsync($"locales/{_currentLang}/{ns}.json");
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            if (dict is not null)
            {
                foreach (var (key, value) in dict)
                    _keys[$"{ns}.{key}"] = value;
            }
        }
        catch
        {
            // Fall back to English or raw key
        }
    }

    public string T(string key, params object[] args)
    {
        if (_keys.TryGetValue(key, out var value))
            return args.Length > 0 ? string.Format(value, args) : value;

        if (_fallbackDict?.TryGetValue(key, out var fallback) == true)
            return args.Length > 0 ? string.Format(fallback, args) : fallback;

        return key;
    }
}
