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

namespace TELEBLASTER_PRO.Controls
{
    /// <summary>
    /// Interaction logic for NumericUpDownControl.xaml
    /// </summary>
    public partial class NumericUpDownControl : UserControl
    {
        public NumericUpDownControl()
        {
            InitializeComponent();
        }

        // Event handler untuk tombol "Up"
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(numericTextBox.Text, out int value))
            {
                numericTextBox.Text = (value + 1).ToString();
            }
        }

        // Event handler untuk tombol "Down"
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(numericTextBox.Text, out int value) && value > 0)
            {
                numericTextBox.Text = (value - 1).ToString();
            }
        }
    }
}
