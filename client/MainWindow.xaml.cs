using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace specify_client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Test.Content = JsonConvert.SerializeObject(MonolithBasicInfo.Create(), Formatting.Indented);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            //Test.Content = "Clicked!";
            this.Close();
        }

        private void TitleBar_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
