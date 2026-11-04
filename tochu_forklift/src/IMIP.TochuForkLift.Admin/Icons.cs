using Microsoft.AspNetCore.Components;

namespace IMIP.TochuForkLift.Admin;

/// <summary>Inline SVGs, same style as WebApp's — kept usable from static (non-interactive) layout markup.</summary>
public static class Icons
{
    public static readonly MarkupString Lift = Svg("""<rect x="4" y="3" width="16" height="18" rx="2"/><path d="M9 13 9 7 M9 7 6.5 9.5 M9 7 11.5 9.5"/><path d="M15 11 15 17 M15 17 12.5 14.5 M15 17 17.5 14.5"/>""");
    public static readonly MarkupString Sun = Svg("""<circle cx="12" cy="12" r="4"/><path d="M12 2v2.5 M12 19.5v2.5 M4.2 4.2l1.8 1.8 M18 18l1.8 1.8 M2 12h2.5 M19.5 12h2.5 M4.2 19.8 6 18 M18 6l1.8-1.8"/>""");
    public static readonly MarkupString Moon = Svg("""<path d="M20 14.5A8.5 8.5 0 1 1 9.5 4a7 7 0 0 0 10.5 10.5Z"/>""");

    private static MarkupString Svg(string inner, string strokeWidth = "1.8") => new(
        $"""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="{strokeWidth}" stroke-linecap="round" stroke-linejoin="round">{inner}</svg>""");
}
