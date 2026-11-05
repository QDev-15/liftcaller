namespace IMIP.TochuForkLift.Shared.Localization;

/// <summary>
/// Scoped per app (WASM: one per browser tab; Blazor Server: one per circuit). Starts at whichever
/// language the host's own appsettings.json configures as default (Localization:DefaultLanguage),
/// and can be switched at runtime — callers are responsible for persisting the choice (localStorage)
/// and restoring it before anything renders, the same way AuthStateService/theme.js do.
/// </summary>
public class Localizer(AppLanguage initial)
{
    public AppLanguage Language { get; private set; } = initial;

    public event Action? LanguageChanged;

    public void SetLanguage(AppLanguage language)
    {
        if (Language == language) return;
        Language = language;
        LanguageChanged?.Invoke();
    }

    /// <summary>Looks up a string by key in the current language. Falls back to Vietnamese, then to the key itself if unknown.</summary>
    public string this[string key] => Strings.Get(key, Language);

    public string Format(string key, params object?[] args) => string.Format(this[key], args);
}
