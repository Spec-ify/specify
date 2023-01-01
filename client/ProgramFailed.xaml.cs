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
    /// Interaction logic for ProgramFailed.xaml
    /// </summary>
    public partial class ProgramFailed : Page
    {
        public ProgramFailed()
        {
            InitializeComponent();
        }
        private void CloseProgram(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Environment.Exit(0);
            }));
        }
    }
}
