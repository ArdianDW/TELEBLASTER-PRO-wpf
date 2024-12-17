﻿using System.Text;
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
using System.Diagnostics;

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
            
            if (!IsLicenseValid())
            {
                ShowLicenseActivation();
            }
            else
            {
                MainContentControl.Content = new Accounts();
            }

            // Tambahkan event handler untuk tombol Help dan Community
            HelpButton.Click += HelpButton_Click;
            CommunityButton.Click += CommunityButton_Click;
        }

        private void InitializePython()
        {
            try
            {
                string pythonEmbeddedPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed");
                string pythonDll = System.IO.Path.Combine(pythonEmbeddedPath, "python310.dll");

                Environment.SetEnvironmentVariable("PYTHONHOME", pythonEmbeddedPath);
                Environment.SetEnvironmentVariable("PYTHONPATH", System.IO.Path.Combine(pythonEmbeddedPath, "Lib"));

                Python.Runtime.Runtime.PythonDLL = pythonDll;

                PythonEngine.Initialize();
                PythonEngine.BeginAllowThreads();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Python: {ex.Message}");
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
                MainOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                StartAnimation("HamburgerAnimation");
                Sidebar.Visibility = Visibility.Collapsed;
                HamburgerButton.Visibility = Visibility.Visible;
                HamburgerButton.Content = "☰"; 
                MainOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            if (Sidebar.Visibility == Visibility.Visible)
            {
                StartAnimation("HamburgerAnimation");
                Sidebar.Visibility = Visibility.Collapsed;
                HamburgerButton.Content = "☰";
                MainOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                StartAnimation("SidebarAnimation");
                Sidebar.Visibility = Visibility.Visible;
                HamburgerButton.Content = "✖";
                MainOverlay.Visibility = Visibility.Visible;
            }
        }

        private void StartAnimation(string animationKey)
        {
            var storyboard = (Storyboard)FindResource(animationKey);
            storyboard.Begin(this);
        }

        private bool IsLicenseValid()
        {
            // Implementasi logika untuk mengecek validitas lisensi
            // Untuk testing, kita anggap lisensi belum valid
            return false; // Ganti dengan logika validasi yang sesuai
        }

        private void ShowLicenseActivation()
        {
            // Tampilkan halaman aktivasi lisensi
            LicenseActivationWindow licenseWindow = new LicenseActivationWindow();
            licenseWindow.ShowDialog();
            if (licenseWindow.IsLicenseActivated)
            {
                MainContentControl.Content = new Accounts();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://wa.me/6281333444233");
        }

        private void CommunityButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://member.jvpartner.id/member");
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open link: {ex.Message}");
            }
        }
    }
}