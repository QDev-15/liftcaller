using System.Windows;
using LiftCall.Shared;

namespace LiftCall.Receiver;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        new ReceiverWindow(ClientSettings.Load()).Show();
    }
}
