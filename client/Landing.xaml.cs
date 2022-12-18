using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Landing : Window
    {
        public Landing()
        {
            InitializeComponent();

            DisplayButtons();

            Application.Current.MainWindow = this;
        }

        public void RunApp()
        {
            Frame.Navigate(new Run());
        }

        private void DisplayButtons()
        {
            Frame.Navigate(new StartButtons());
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {

            // Code from https://stackoverflow.com/a/10238715 
            // and originally from http://softwareindexing.blogspot.com/2008/12/wpf-hyperlink-open-browser.html, thanks eandersson and Max! - K97i

            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;

        }

    }
}
