using System.Windows;
using LiftCall.Shared;

namespace LiftCall.Caller;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = ClientSettings.Load();

        // 起動引数で号機番号を上書き（同じPCで複数号機を試すとき用）
        int unitNo = settings.UnitNo;
        if (e.Args.Length > 0 && int.TryParse(e.Args[0], out var n) && n > 0)
            unitNo = n;

        new CallerWindow(unitNo, settings).Show();
    }
}
