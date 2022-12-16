using System;
using System.Collections.Generic;
using System.Management;
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
        [STAThread]
        public void Greenify(string objective)
        {
            
            switch (objective)
            {
                case "MainDataText":
                    MainDataText.Foreground = new SolidColorBrush(Colors.Green);
                    break;

                case "SystemDataText":
                    SystemDataText.Foreground = new SolidColorBrush(Colors.Green);
                    break;

                case "HardwareInfoText":
                    HardwareDataText.Foreground = new SolidColorBrush(Colors.Green);
                    break;

                case "SecurityInfoText":
                    SecurityDataText.Foreground = new SolidColorBrush(Colors.Green);
                    break;

                case "NetworkInfoText":
                    NetworkDataText.Foreground = new SolidColorBrush(Colors.Green);
                    break;

                case "AssembleText":
                    AssembleText.Foreground = new SolidColorBrush(Colors.Green);
                    break;

                case "WriteFileText":
                    WriteFileText.Foreground = new SolidColorBrush(Colors.Green);
                    break;

                default:
                    throw new ArgumentException();
            
            }
        }
    }
}
