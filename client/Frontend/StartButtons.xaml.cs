using System.Windows;
using System.Windows.Controls;

namespace specify_client;

/// <summary>
/// Interaction logic for StartButtons.xaml
/// </summary>
public partial class StartButtons : Page
{
    public StartButtons()
    {
        InitializeComponent();
    }

    private void UploadOff(object sender, RoutedEventArgs e)
    {
        Settings.DontUpload = true;
    }

    private void UploadOn(object sender, RoutedEventArgs e)
    {
        Settings.DontUpload = false;
    }

    private void UsernameOn(object sender, RoutedEventArgs e)
    {
        Settings.RedactUsername = true;
    }

    private void UsernameOff(object sender, RoutedEventArgs e)
    {
        Settings.RedactUsername = false;
    }

    private void OneDriveOn(object sender, RoutedEventArgs e)
    {
        Settings.RedactOneDriveCommercial = true;
    }

    private void OneDriveOff(object sender, RoutedEventArgs e)
    {
        Settings.RedactOneDriveCommercial = false;
    }

    private void DebugLogToggleOn(object sender, RoutedEventArgs e)
    {
        Settings.EnableDebug = true;
    }

    private void DebugLogToggleOff(object sender, RoutedEventArgs e)
    {
        Settings.EnableDebug = false;
    }

    private async void StartAction(object sender, RoutedEventArgs e)
    {
        var main = App.Current.MainWindow as Landing;
        await main.RunApp();
    }
}