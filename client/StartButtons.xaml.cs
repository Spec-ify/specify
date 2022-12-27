using HidSharp.Reports.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
    }
}
