namespace IMIP.TochuForkLift.Api.Services;

/// <summary>Bound from appsettings.json "SignalLight" section — plan.md §6.1.</summary>
public class SignalLightOptions
{
    public const string SectionName = "SignalLight";

    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = "192.168.55.7";
    public int TimeoutSeconds { get; set; } = 2;
}
