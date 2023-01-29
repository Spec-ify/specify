using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;

namespace specify_client
{
    /// <summary>
    /// Interaction logic for ProgramFinalizeNoUpload.xaml
    /// </summary>
    public partial class ProgramFinalizeNoUpload : Page
    {
        public ProgramFinalizeNoUpload()
        {
            InitializeComponent();

            SystemSounds.Asterisk.Play();

            this.Focus();
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