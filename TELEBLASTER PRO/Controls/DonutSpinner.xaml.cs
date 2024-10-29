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
using System.Windows.Media.Animation;

namespace TELEBLASTER_PRO.Controls
{
    /// <summary>
    /// Interaction logic for DonutSpinner.xaml
    /// </summary>
    public partial class DonutSpinner : UserControl
    {
        public DonutSpinner()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SpinnerColorProperty =
            DependencyProperty.Register("SpinnerColor", typeof(Brush), typeof(DonutSpinner), new PropertyMetadata(Brushes.Black));

        public Brush SpinnerColor
        {
            get { return (Brush)GetValue(SpinnerColorProperty); }
            set { SetValue(SpinnerColorProperty, value); }
        }

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(Duration), typeof(DonutSpinner), new PropertyMetadata(new Duration(TimeSpan.FromSeconds(1))));

        public Duration Duration
        {
            get { return (Duration)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }
    }
}
