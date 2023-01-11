using System;
using System.Windows;
using System.Windows.Controls;

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
