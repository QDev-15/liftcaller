using Microsoft.AspNetCore.Components;

namespace IMIP.TochuForkLift.WebApp;

/// <summary>Inline SVGs ported verbatim from the approved prototype (plan.md §6.3).</summary>
public static class Icons
{
    public static readonly MarkupString Lift = Svg("""<rect x="4" y="3" width="16" height="18" rx="2"/><path d="M9 13 9 7 M9 7 6.5 9.5 M9 7 11.5 9.5"/><path d="M15 11 15 17 M15 17 12.5 14.5 M15 17 17.5 14.5"/>""");
    public static readonly MarkupString Box = Svg("""<path d="M12 3 21 7.5 21 16.5 12 21 3 16.5 3 7.5 Z"/><path d="M3 7.5 12 12 21 7.5"/><path d="M12 12 12 21"/>""");
    public static readonly MarkupString Return = Svg("""<path d="M4 12a8 8 0 0 1 13.66-5.66L20 8"/><path d="M20 4v4h-4"/><path d="M20 12a8 8 0 0 1-13.66 5.66L4 16"/><path d="M4 20v-4h4"/>""");
    public static readonly MarkupString Warn = Svg("""<path d="M12 3.5 22 20.5 2 20.5 Z"/><path d="M12 9.5v5"/><circle cx="12" cy="17.3" r="0.6" fill="currentColor" stroke="none"/>""");
    public static readonly MarkupString Check = Svg("""<path d="M20 6 9 17 4 12"/>""", strokeWidth: "2.4");
    public static readonly MarkupString X = Svg("""<path d="M6 6 18 18 M18 6 6 18"/>""", strokeWidth: "2.2");
    public static readonly MarkupString Power = Svg("""<path d="M12 3v8"/><path d="M6.3 6.3a8 8 0 1 0 11.4 0"/>""", strokeWidth: "2");

    private static MarkupString Svg(string inner, string strokeWidth = "1.8") => new(
        $"""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="{strokeWidth}" stroke-linecap="round" stroke-linejoin="round">{inner}</svg>""");
}
