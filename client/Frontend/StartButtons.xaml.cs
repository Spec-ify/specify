using System.Windows;
using System.Windows.Controls;
using System;
using System.Windows.Media;

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

    private void SettingsOn(object sender, RoutedEventArgs e)
    {
        MainText.Visibility = Visibility.Hidden;
        SettingsToggles.Visibility = Visibility.Visible;
    }

    private void SettingsOff(object sender, RoutedEventArgs e)
    {
        SettingsToggles.Visibility = Visibility.Hidden;
        MainText.Visibility = Visibility.Visible;
    }

    private async void StartAction(object sender, RoutedEventArgs e)
    {
        try
        {
            var main = App.Current.MainWindow as Landing;
            await main.RunApp();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(@"specify_hardfail.log", $"{ex}");
            System.Environment.Exit(-1);
        }
    }
}