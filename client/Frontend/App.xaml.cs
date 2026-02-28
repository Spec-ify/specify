using specify_client.data;
using System.Windows;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace specify_client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    [DllImport("Kernel32.dll")]
    public static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [STAThread]
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        // Calling GetSystemMetrics with value 67 asks Windows if it's running in safe mode.
        // Returns 0 for a standard boot, 1 for Safe Mode, 2 for Safe Mode w/ Networking.
        // Specify cannot properly gather information in safe mode and must be stopped prior to operation.
        var bootType = Interop.GetSystemMetrics(67);

        if (e.Args.Contains("-h"))
        {
            Settings.Headless = true;
            AllocConsole();

            if (bootType != 0)
            {
                Console.WriteLine("Specify cannot be run in Safe Mode.");
                Environment.Exit(-1);
            }

            try
            {
                Utils.GetWmi("Win32_OperatingSystem");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Specify is unable to communicate with the Windows Management Instrumentation.\n{ex}");
                Environment.Exit(-1);
            }

            foreach (var item in e.Args)
            {
                Console.WriteLine(item);
                switch (item)
                {
                    case "-redact-username":
                        Settings.RedactUsername = true;
                        break;

                    case "-redact-sn":
                        Settings.RedactSerialNumber = true;
                        break;

                    case "-redact-odc":
                        Settings.RedactOneDriveCommercial = true;
                        break;

                    case "-dont-upload":
                        Settings.DontUpload = true;
                        break;

                    default:
                        break;
                }
            }

            // RUN PROGRAM
            try
            {
                await Program.Main();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(@"specify_hardfail.log", $"{ex}");
            }

            Console.ReadKey();
            FreeConsole();
            Environment.Exit(0);
        }

        else
        {

            if (bootType != 0)
            {
                MessageBox.Show("Specify cannot be run in Safe Mode.", "Specify", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }

            // Check that the WMI is functioning prior to loading the app.
            // Specify is critically dependent on a functioning WMI Service. If it cannot talk to the WMI for whatever reason, Specify cannot run.
            try
            {
                Utils.GetWmi("Win32_OperatingSystem");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Specify is unable to communicate with the Windows Management Instrumentation.\n{ex}", "Specify", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }

            StartupUri = new Uri("/specify_client;component/Frontend/Landing.xaml", UriKind.Relative);
        }
    }
}