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

namespace specify_client.Frontend
{
    /// <summary>
    /// Interaction logic for CheckboxUserControl.xaml
    /// </summary>
    public partial class CheckboxUserControl : UserControl
    {
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                name: "IsChecked",
                propertyType: typeof(bool),
                ownerType: typeof(CheckboxUserControl),
                typeMetadata: new FrameworkPropertyMetadata(
                    defaultValue: false,
                    flags: FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                name: "Content",
                propertyType: typeof(object),
                ownerType: typeof(CheckboxUserControl));

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }
        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public CheckboxUserControl()
        {
            InitializeComponent();
        }
    }
}
