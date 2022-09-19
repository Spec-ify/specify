using System.Windows;
using System.Windows.Media;

namespace specify_client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public SolidColorBrush nord6;
        public App()
        {
            nord6 = new SolidColorBrush(Color.FromRgb(0xec, 0xef, 0xf4));
        }
    }
}