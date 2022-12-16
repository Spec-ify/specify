using System.Windows.Controls;

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
