using Wpf.Ui.Controls;

namespace ClaudeSessionManager;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RootNavigationView.Navigate(typeof(Views.SessionsPage));
    }
}
