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
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


        private void Test_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Emoji Picker button clicked");
        }
        
        private void ExtractContactsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (Py.GIL())
                {
                    dynamic py = Py.Import("functions");
                    var result = py.extract_contacts_to_csv("user1.session", "contacts.csv");
                    bool success = result[0];
                    string message = result[1];

                    MessageBox.Show(message, success ? "Success" : "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error");
            }
        }
    }
}
