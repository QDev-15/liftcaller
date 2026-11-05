namespace IMIP.TochuForkLift.Shared.Localization;

/// <summary>
/// ja = Japanese (the original customer's own language — CLAUDE.md §5.12: "UI tiếng Nhật, có sẵn
/// text tiếng Anh" — Japanese is the primary UI language), en = English (the approved prototype's
/// wording — https://claude.ai/artifact/YTML5Ag1GbGPu7nwRvPa5r, offered alongside it). Only these
/// two are supported — Vietnamese was dropped per explicit instruction.
/// </summary>
public enum AppLanguage
{
    En,
    Ja
}

public static class AppLanguageExtensions
{
    public static string ToCode(this AppLanguage lang) => lang switch
    {
        AppLanguage.Ja => "ja",
        _ => "en"
    };

    public static AppLanguage ParseCode(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "ja" => AppLanguage.Ja,
        "en" => AppLanguage.En,
        _ => AppLanguage.Ja
    };
}
