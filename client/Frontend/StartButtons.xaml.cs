using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace specify_client
{
    /// <summary>
    /// Interaction logic for StartButtons.xaml
    /// </summary>
    public partial class StartButtons : Page
    {
        public StartButtons()
        {
            InitializeComponent();
        }

        private void UploadOn(object sender, RoutedEventArgs e)
        {
            Settings.DontUpload = true;
            UploadToggle.Background = new SolidColorBrush(Colors.Green);
        }
        private void UploadOff(object sender, RoutedEventArgs e)
        {
            Settings.DontUpload = false;
            UploadToggle.Background = new SolidColorBrush(Colors.White);
        }
        private void UsernameOn(object sender, RoutedEventArgs e)
        {
            Settings.RedactUsername = true;
            Username.Background = new SolidColorBrush(Colors.Green);
        }
        private void UsernameOff(object sender, RoutedEventArgs e)
        {
            Settings.RedactUsername = false;
            Username.Background = new SolidColorBrush(Colors.White);
        }
        private void OneDriveOn(object sender, RoutedEventArgs e)
        {
            Settings.RedactOneDriveCommercial = true;
            OneDriveToggle.Background = new SolidColorBrush(Colors.Green);
        }
        private void OneDriveOff(object sender, RoutedEventArgs e)
        {
            Settings.RedactOneDriveCommercial = false;
            OneDriveToggle.Background = new SolidColorBrush(Colors.White);
        }
        private void StartAction(object sender, RoutedEventArgs e)
        {
            var main = App.Current.MainWindow as Landing;
            main.RunApp();
        }
        private void DebugLogToggleOn(object sender, RoutedEventArgs e)
        {
            Settings.EnableDebug = true;
            DebugLogToggle.Background = new SolidColorBrush(Colors.Green);
        }
        private void DebugLogToggleOff(object sender, RoutedEventArgs e)
        {
            Settings.EnableDebug = false;
            DebugLogToggle.Background = new SolidColorBrush(Colors.White);
        }
    }
}
