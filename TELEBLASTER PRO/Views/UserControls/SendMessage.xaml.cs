using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
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
using Python.Runtime;
using TELEBLASTER_PRO.ViewModels;

namespace TELEBLASTER_PRO.Views.UserControls
{
    /// <summary>
    /// Interaction logic for SendMessage.xaml
    /// </summary>
    public partial class SendMessage : UserControl
    {
        public SendMessage()
        {
            InitializeComponent();
            var viewModel = new SendMessageViewModel(new AccountViewModel());
            DataContext = viewModel;
            System.Diagnostics.Debug.WriteLine($"DataContext is set to: {DataContext.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"IsCheckedAll initial value: {viewModel.IsCheckedAll}");
        }

    }
}
