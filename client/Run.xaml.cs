using System;
using System.Collections.Generic;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace specify_client
{
    /// <summary>
    /// Interaction logic for Run.xaml
    /// </summary>

    public partial class Run : Page
    {
        public Run()
        {
            InitializeComponent();

            Program.Main();
        }

        public void Greenify(string objective)
        {
            var txt = StackPanel1.FindName(objective);

            if (txt != null)
            {
                txt = StackPanel2.FindName(objective);
            }

            TextBlock sometxt = txt as TextBlock;
            sometxt.Foreground = new SolidColorBrush(Colors.Green);
        }
    }
}
