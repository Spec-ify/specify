using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Diagnostics;
using System.Security.AccessControl;

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
    }
}
