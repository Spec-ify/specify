using System.Windows;
using System.Windows.Controls;

namespace specify_client.Frontend
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsUserControl : UserControl
    {
        public static readonly DependencyProperty DontUploadProperty =
            DependencyProperty.Register(
                name: "DontUpload",
                propertyType: typeof(bool),
                ownerType: typeof(SettingsUserControl),
                typeMetadata: new FrameworkPropertyMetadata(
                    defaultValue: false,
                    flags: FrameworkPropertyMetadataOptions.AffectsRender,
                    new PropertyChangedCallback(DontUploadChanged)));

        public bool DontUpload
        {
            get => (bool)GetValue(DontUploadProperty);
            set => SetValue(DontUploadProperty , value);
        }

        public static void DontUploadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SettingsUserControl)d).DontUpload = (bool)e.NewValue;
            Settings.DontUpload = (bool)e.NewValue;
        }

        public SettingsUserControl()
        {
            InitializeComponent();
        }

        private void UsernameOn(object sender, RoutedEventArgs e)
        {
            Settings.RedactUsername = true;
        }

        private void UsernameOff(object sender, RoutedEventArgs e)
        {
            Settings.RedactUsername = false;
        }

        private void SerialNumberOn(object sender, RoutedEventArgs e)
        {
            Settings.RedactSerialNumber = true;
        }

        private void SerialNumberOff(object sender, RoutedEventArgs e)
        {
            Settings.RedactSerialNumber = false;
        }

        private void RedactNetworkingOn(object sender, RoutedEventArgs e)
        {
            Settings.RedactNetworking = true;
        }

        private void RedactNetworkingOff(object sender, RoutedEventArgs e)
        {
            Settings.RedactNetworking = false;
        }

        private void DebugLogToggleOn(object sender, RoutedEventArgs e)
        {
            Settings.EnableDebug = true;
        }

        private void DebugLogToggleOff(object sender, RoutedEventArgs e)
        {
            Settings.EnableDebug = false;
        }
    }
}
