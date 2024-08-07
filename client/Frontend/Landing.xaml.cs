using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace specify_client;

public partial class Landing : Window
{
    public Landing()
    {
        InitializeComponent();

        DisplayButtons();

        Application.Current.MainWindow = this;
    }

    public async Task RunApp()
    {
        Frame.Navigate(new Run());
        await Program.Main();
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
        this.Topmost = false;
        this.Focus();
    }

    public void ProgramFinalizeNoUpload()
    {
        Process.Start("explorer.exe", "/select, \"" + AppDomain.CurrentDomain.BaseDirectory + "specify_specs.json\"");

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(ProgramFinalizeNoUpload));
            return;
        }

        Frame.Navigate(new ProgramFinalizeNoUpload());

        this.Activate();
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
        this.Topmost = false;
        this.Focus();
    }

    public void ProgramFailed()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(ProgramFailed));
            return;
        }

        Frame.Navigate(new ProgramFailed());

        this.Activate();
        this.Topmost = false;
        this.Focus();
    }
}