using IMIP.TochuForkLift.Shared.Localization;
using Microsoft.AspNetCore.Components;

namespace IMIP.TochuForkLift.WebApp.Components;

/// <summary>
/// Base for any component that reads Loc[...] directly in its markup. Subscribes to
/// Localizer.LanguageChanged itself rather than relying on the parent's re-render cascading down to
/// it — observed in practice that some leaf components (e.g. LiftToggle) didn't pick up a live
/// language switch even though sibling components on the same page did.
/// </summary>
public abstract class LocalizedComponentBase : ComponentBase, IDisposable
{
    [Inject] protected Localizer Loc { get; set; } = null!;

    protected override void OnInitialized()
    {
        Loc.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged() => InvokeAsync(StateHasChanged);

    public virtual void Dispose()
    {
        Loc.LanguageChanged -= OnLanguageChanged;
    }
}
