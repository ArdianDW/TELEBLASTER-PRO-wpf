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
using TELEBLASTER_PRO.ViewModels;

namespace TELEBLASTER_PRO.Views.UserControls
{
    /// <summary>
    /// Interaction logic for GroupFinder.xaml
    /// </summary>
    public partial class GroupFinder : UserControl
    {
        public GroupFinder()
        {
            InitializeComponent();
            DataContext = new GroupFinderViewModel(); 
        }

        private void CheckAll_Checked(object sender, RoutedEventArgs e)
        {
            var viewModel = (GroupFinderViewModel)DataContext;
            viewModel.IsCheckedAll = true; 
        }

        private void CheckAll_Unchecked(object sender, RoutedEventArgs e)
        {
            var viewModel = (GroupFinderViewModel)DataContext;
            viewModel.IsCheckedAll = false; 
        }
    }
}
