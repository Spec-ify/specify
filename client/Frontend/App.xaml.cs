using System.Windows;

namespace specify_client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Calling GetSystemMetrics with value 67 asks Windows if it's running in safe mode.
        // Returns 0 for a standard boot, 1 for Safe Mode, 2 for Safe Mode w/ Networking.
        // Specify cannot properly gather information in safe mode and must be stopped prior to operation.
        var bootType = Interop.GetSystemMetrics(67);
        if(bootType != 0)
        {
            MessageBox.Show("Specify cannot be run in Safe Mode.", "Specify", MessageBoxButton.OK, MessageBoxImage.Error);
            System.Environment.Exit(0);
        }
    }
}