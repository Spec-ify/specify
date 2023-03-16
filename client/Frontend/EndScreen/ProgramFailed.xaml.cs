using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace specify_client;

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

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        // Code from https://stackoverflow.com/a/10238715
        // and originally from http://softwareindexing.blogspot.com/2008/12/wpf-hyperlink-open-browser.html, thanks eandersson and Max! - K97i

        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        e.Handled = true;
    }
}