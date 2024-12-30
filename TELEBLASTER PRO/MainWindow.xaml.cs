﻿using System;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Net.Http;
using TELEBLASTER_PRO.Views.UserControls;
using TELEBLASTER_PRO.Models;
using Python.Runtime;
using System.Diagnostics;
using AutoUpdaterDotNET;
using System.Reflection;

namespace TELEBLASTER_PRO
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string ApplicationVersion { get; set; }
        private bool _isUpdating = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            DataContext = this;
            InitializePython();
            this.SizeChanged += MainWindow_SizeChanged;

            ShowLicenseActivation();
            CheckForUpdates();

            HelpButton.Click += HelpButton_Click;
            CommunityButton.Click += CommunityButton_Click;

            RefreshAccountsAsync();
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
            return false;
        }

        private void ShowLicenseActivation()
        {
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

        private void CheckForUpdates()
        {
            Debug.WriteLine("Starting update check...");

            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Tampilkan spinner untuk "Checking update"
                    UpdateStatusTextBlock.Text = "Checking update...";
                    UpdateSpinner.Visibility = Visibility.Visible;

                    AutoUpdater.OpenDownloadPage = true;
                    AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
                    AutoUpdater.ApplicationExitEvent += AutoUpdaterOnApplicationExitEvent;

                    try
                    {
                        AutoUpdater.Start("https://www.dropbox.com/scl/fi/w228u9rt9kegj9qxohsdp/update.xml?rlkey=qm48exydhbxgkmfctu6tcjrdz&st=bgyg1axk&raw=1");
                        Debug.WriteLine("AutoUpdater started. Waiting for response...");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception while fetching update.xml: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during update check: {ex.Message}");
            }
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            Debug.WriteLine("AutoUpdaterOnCheckForUpdateEvent triggered.");

            if (args == null)
            {
                Debug.WriteLine("Failed to fetch update.xml. AutoUpdater.NET can't reach the XML file on the server.");
                return;
            }

            Debug.WriteLine("Successfully reached the update.xml URL.");

            string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Debug.WriteLine($"Current application version: {currentVersion}");

            Debug.WriteLine($"Version in update.xml: {args.CurrentVersion ?? "null"}");
            Debug.WriteLine($"Download URL: {args.DownloadURL ?? "null"}");
            Debug.WriteLine($"Is Update Available: {args.IsUpdateAvailable}");

            if (!args.IsUpdateAvailable)
            {
                Debug.WriteLine("No update available. Current version is up to date.");
                Dispatcher.Invoke(() =>
                {
                    UpdateSpinner.Visibility = Visibility.Collapsed; // Sembunyikan spinner
                });
            }
            else
            {
                Debug.WriteLine($"Update available. Current version: {currentVersion}, New version: {args.CurrentVersion}");

                // Tampilkan MessageBox untuk konfirmasi pembaruan
                var result = MessageBox.Show("A new update is available. Do you want to update now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _isUpdating = true;

                    Dispatcher.Invoke(() =>
                    {
                        UpdateStatusTextBlock.Text = "Downloading update...";
                        UpdateSpinner.Visibility = Visibility.Visible; // Tampilkan spinner untuk "Downloading update"
                    });

                    DownloadAndReplaceExe(args.DownloadURL);

                    // Return to prevent further execution
                    return;
                }
                else
                {
                    Debug.WriteLine("User chose not to update.");
                    Dispatcher.Invoke(() =>
                    {
                        UpdateSpinner.Visibility = Visibility.Collapsed; // Sembunyikan spinner jika pengguna menolak pembaruan
                    });
                }
            }
        }

        private async void DownloadAndReplaceExe(string downloadUrl)
        {
            string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TELEBLASTER PRO.exe");
            Debug.WriteLine($"Temporary file path: {tempFilePath}");

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Debug.WriteLine($"Starting download from: {downloadUrl}");
                    HttpResponseMessage response = await client.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                    Debug.WriteLine("Download completed successfully.");

                    using (var fs = new System.IO.FileStream(tempFilePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                        Debug.WriteLine("File saved to temporary path.");
                    }

                    string currentExePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string backupExePath = currentExePath + ".bak";
                    string batchFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "update.bat");

                    Debug.WriteLine($"Current exe path: {currentExePath}");
                    Debug.WriteLine($"Backup exe path: {backupExePath}");
                    Debug.WriteLine($"Batch file path: {batchFilePath}");
                    

                    using (StreamWriter writer = new StreamWriter(batchFilePath))
                    {
                        writer.WriteLine("@echo off");
                        writer.WriteLine("timeout /t 3 /nobreak > nul");
                        writer.WriteLine($"move /y \"{tempFilePath}\" \"{currentExePath}\"");
                        writer.WriteLine($"start \"\" \"{currentExePath}\"");
                        writer.WriteLine("exit");
                    }



                    // Jalankan file batch
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batchFilePath,
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });

                    // Tutup aplikasi saat ini
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during download or replacement: {ex.Message}");
                    MessageBox.Show($"Error downloading or replacing update: {ex.Message}", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    Dispatcher.Invoke(() =>
                    {
                        UpdateSpinner.Visibility = Visibility.Collapsed; // Sembunyikan spinner jika terjadi kesalahan
                    });
                }
            }
        }

        private void AutoUpdaterOnApplicationExitEvent()
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatusTextBlock.Text = "Installing update...";
            });
        }

        private async Task RefreshAccountsAsync()
        {
            await Task.Run(() =>
            {
                Debug.WriteLine("RefreshAccountsAsync method called in MainWindow");

                bool refreshSuccessful = false;
                while (!refreshSuccessful)
                {
                    try
                    {
                        var accounts = new List<Account>();
                        var accountsFromDb = Account.GetAccountsFromDatabase();
                        foreach (var account in accountsFromDb)
                        {
                            account.Status = CheckAccountLoginAsync(account.SessionName).Result ? "Active" : "Inactive";
                            account.UpdateStatusInDatabase();
                            accounts.Add(account);
                        }
                        refreshSuccessful = true;
                    }
                    catch (PythonException pe)
                    {
                        if (pe.Message.Contains("set_wakeup_fd only works in main thread of the main interpreter"))
                        {
                            Debug.WriteLine("Retrying refresh due to Python error: " + pe.Message);
                            Task.Delay(1000).Wait(); // Optional delay before retrying
                        }
                        else
                        {
                            Debug.WriteLine("An error occurred during refresh: " + pe.Message);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("An error occurred during refresh: " + ex.Message);
                        break;
                    }
                }

                Debug.WriteLine("Refresh completed in MainWindow");
            });
        }

        private async Task<bool> CheckAccountLoginAsync(string sessionName)
        {
            return await Task.Run(() =>
            {
                bool isLoggedIn = false;
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        using (Py.GIL())
                        {
                            dynamic py = Py.Import("Backend.functions");
                            isLoggedIn = py.check_account_login_sync(sessionName);
                        }
                    });
                    System.Diagnostics.Debug.WriteLine($"Status login akun {sessionName}: {(isLoggedIn ? "Aktif" : "Tidak Aktif")}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saat memeriksa status login untuk {sessionName}: {ex.Message}");
                }
                return isLoggedIn;
            });
        }
    }
}