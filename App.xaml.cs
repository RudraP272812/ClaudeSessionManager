using System.Windows;

namespace ClaudeSessionManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        new Views.SplashWindow().Show();
    }
}
