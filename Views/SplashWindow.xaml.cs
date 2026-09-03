using System.Windows;
using System.Windows.Media.Animation;
using ClaudeSessionManager.Services;

namespace ClaudeSessionManager.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => ((Storyboard)Resources["IntroStoryboard"]).Begin();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartButton.Visibility = Visibility.Collapsed;
        LoadingPanel.Visibility = Visibility.Visible;

        try
        {
            await SessionRepository.ScanAsync();
        }
        catch
        {
            // Best-effort warm-up scan only - the main window performs its own scan
            // and will surface any real failure there.
        }

        new MainWindow().Show();
        Close();
    }
}
