using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace specify_client;

public class MainWindowViewModel
{
    public string TestContent { get; set; }

    public MainWindowViewModel()
    {
        TestContent = (new Monolith
        {
            BasicInfo = MonolithBasicInfo.Create()
        }).Serialize();
    }
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static SolidColorBrush Red = new SolidColorBrush(Color.FromRgb(0xbf, 0x61, 0x6a));
    public static SolidColorBrush Nord6 = new SolidColorBrush(Color.FromRgb(0xec, 0xef, 0xf4));
    private string TestContent { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        Control.Template = (ControlTemplate) Resources["StartView"];
        CloseButton.Background = Brushes.Transparent;

        DataContext = new MainWindowViewModel();
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

    private void CloseButton_OnMouseEnter(object sender, MouseEventArgs e)
    {
        ClosePath.Fill = Nord6;
    }

    private void CloseButton_OnMouseLeave(object sender, MouseEventArgs e)
    {
        ClosePath.Fill = Red;
    }
}
