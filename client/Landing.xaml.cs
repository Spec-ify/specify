using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace specify_client
{

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
        public void ProgramFinalize()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(ProgramFinalize));
                return;
            }

            Frame.Navigate(new ProgramFinalized());
            this.Activate();
            this.WindowState = System.Windows.WindowState.Normal;
            this.Topmost = false;
            this.Focus();
        }
        public void ProgramFinalizeNoUpload()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(ProgramFinalizeNoUpload));
                return;
            }

            Frame.Navigate(new ProgramFinalizeNoUpload());
            
            this.Activate();
            this.WindowState = WindowState.Normal;
            this.Topmost = false;
            this.Focus();
        }
        public void UploadFailed()
        {

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(UploadFailed));
                return;
            }

            Frame.Navigate(new UploadFail());

            this.Activate();
            this.WindowState = System.Windows.WindowState.Normal;
            this.Topmost = false;
            this.Focus();
        }
        public void ProgramFail()
        {

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(ProgramFail));
                return;
            }

            Frame.Navigate(new ProgramFailed());

            this.Activate();
            this.WindowState = System.Windows.WindowState.Normal;
            this.Topmost = false;
            this.Focus();
        }

    }
}
