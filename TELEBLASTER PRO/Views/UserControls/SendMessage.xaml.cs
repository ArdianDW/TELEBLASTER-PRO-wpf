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

            viewModel.RequestFocusOnTextBox += () => CustomTextBox.Focus();
        }

        private void CheckAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var contact in ((SendMessageViewModel)DataContext).ContactsList)
            {
                contact.IsChecked = true;
            }
        }

        private void CheckAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var contact in ((SendMessageViewModel)DataContext).ContactsList)
            {
                contact.IsChecked = false;
            }
        }

        private void SelectRange_Click(object sender, RoutedEventArgs e)
        {
            int startIndex = (int)StartIndex.Value - 1; // Convert to zero-based index
            int endIndex = (int)EndIndex.Value - 1;

            var contactsList = ((SendMessageViewModel)DataContext).ContactsList;
            for (int i = startIndex; i <= endIndex && i < contactsList.Count; i++)
            {
                contactsList[i].IsChecked = true;
            }
        }

        private void ResetSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (var contact in ((SendMessageViewModel)DataContext).ContactsList)
            {
                contact.IsChecked = false;
            }
        }
    }
}
