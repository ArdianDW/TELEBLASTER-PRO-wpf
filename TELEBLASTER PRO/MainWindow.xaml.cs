using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TELEBLASTER_PRO.Views.UserControls;
using Python.Runtime;

namespace TELEBLASTER_PRO
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializePython();
            this.SizeChanged += MainWindow_SizeChanged;
            
            MainContentControl.Content = new Accounts();
        }

        private void InitializePython()
        {
            try
            {
                string pythonPath = @"c:\Users\ardia\AppData\Local\Programs\Python\Python310";
                string pythonDll = System.IO.Path.Combine(pythonPath, "python310.dll");

                Environment.SetEnvironmentVariable("PYTHONHOME", pythonPath);
                Environment.SetEnvironmentVariable("PYTHONPATH", pythonPath);

                Python.Runtime.Runtime.PythonDLL = pythonDll;

                PythonEngine.Initialize();
                PythonEngine.BeginAllowThreads();

                MessageBox.Show("Python berhasil diinisialisasi");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menginisialisasi Python: {ex.Message}");
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized || this.Width >= 1033)
            {
                StartAnimation("SidebarAnimation");
                Sidebar.Visibility = Visibility.Visible;
                HamburgerButton.Visibility = Visibility.Collapsed;
                Sidebar.SetValue(Grid.ColumnSpanProperty, 1);
            }
            else
            {
                StartAnimation("HamburgerAnimation");
                Sidebar.Visibility = Visibility.Collapsed;
                HamburgerButton.Visibility = Visibility.Visible;
                HamburgerButton.Content = "☰"; 
            }
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            if (Sidebar.Visibility == Visibility.Visible)
            {
                StartAnimation("HamburgerAnimation");
                Sidebar.Visibility = Visibility.Collapsed;
                HamburgerButton.Content = "☰";
            }
            else
            {
                StartAnimation("SidebarAnimation");
                Sidebar.Visibility = Visibility.Visible;
                HamburgerButton.Content = "✖";
            }
        }

        private void StartAnimation(string animationKey)
        {
            var storyboard = (Storyboard)FindResource(animationKey);
            storyboard.Begin(this);
        }
    }
}
