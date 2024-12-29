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
using System.Windows.Shapes;
using System.Management;
using System.Security.Cryptography;
using System.Diagnostics;
using TELEBLASTER_PRO.Helpers;
using System.Net.Http;
using Newtonsoft.Json;
using System.Xml;
using System.Windows.Threading;
using System.Reflection;

namespace TELEBLASTER_PRO
{
    /// <summary>
    /// Interaction logic for LicenseActivationWindow.xaml
    /// </summary>
    public partial class LicenseActivationWindow : Window
    {
        public bool IsLicenseActivated { get; private set; }
        private DispatcherTimer downloadTimer;
        private int downloadProgress;
        private bool _isUpdating = false;

        public LicenseActivationWindow()
        {
            InitializeComponent();
            CheckLicenseStatus();
            string hardwareId = GetHardwareId();
            Debug.WriteLine($"Hardware ID: {hardwareId}");
        }

        private string GetHardwareId()
        {
            string cpuId = GetCpuId();
            if (string.IsNullOrEmpty(cpuId))
            {
                return null;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(cpuId));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private string GetCpuId()
        {
            try
            {
                using (ManagementClass mc = new ManagementClass("Win32_Processor"))
                {
                    foreach (ManagementObject mo in mc.GetInstances())
                    {
                        return mo.Properties["ProcessorId"].Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error : {ex.Message}");
            }
            return null;
        }

        private async Task<bool> ValidateEmailAsync(string email)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = $"https://member.jvpartner.id/api/check-access/by-email?_key=46QEtGKmHYTvfBZwsAkM&email={email}";

                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();

                    dynamic result = JsonConvert.DeserializeObject(responseBody);
                    return result.ok;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error validating email: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task<bool> ValidateLicenseAsync(string email, string licenseKey, string hardwareId)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = $"https://member.jvpartner.id/softsale/api/activate?key={licenseKey}&request[hardware-id]={hardwareId}";

                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseBody);

                    if (result.code == "ok")
                    {
                        string expires = result.license_expires;
                        DatabaseConnection.SaveLicense(email, licenseKey, expires);
                        return true;
                    }
                    return false;
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show("Internet connection is not available. Please check your connection and try again.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error validating license: {ex.Message}");
                    return false;
                }
            }
        }

        private async void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string licenseKey = LicenseKeyTextBox.Text;
            string hardwareId = GetHardwareId();

            // Check license first
            if (!await CheckLicenseAsync(licenseKey))
            {
                return;
            }

            if (!await ValidateEmailAsync(email))
            {
                MessageBox.Show("Email not valid!!");
                return;
            }

            if (await ValidateLicenseAsync(email, licenseKey, hardwareId))
            {
                IsLicenseActivated = true;
                MessageBox.Show("License valid!!");

                this.Close();
            }
            else
            {
                MessageBox.Show("License not valid!!");
            }
        }

        private async void CheckLicenseStatus()
        {
            if (_isUpdating) return;

            if (LoadActivationStatus())
            {
                CheckingGrid.Visibility = Visibility.Visible;
                InputGrid.Visibility = Visibility.Collapsed;

                string email = DatabaseConnection.GetEmailFromDatabase();
                string licenseKey = DatabaseConnection.GetLicenseKeyFromDatabase();
                string hardwareId = GetHardwareId();

                if (!await CheckLicenseAsync(licenseKey))
                {
                    ShowInputGrid();
                    return;
                }

                if (!await ValidateEmailAsync(email))
                {
                    MessageBox.Show("Email not valid");
                    ShowInputGrid();
                    return;
                }

                if (!await CheckActivationAsync(licenseKey, hardwareId))
                {
                    MessageBox.Show("License not valid");
                    ShowInputGrid();
                    return;
                }
                IsLicenseActivated = true;
                this.Close();
            }
            else
            {
                ShowInputGrid();
            }
        }

        private async Task<bool> CheckActivationAsync(string licenseKey, string hardwareId)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = $"https://member.jvpartner.id/softsale/api/check-activation?key={licenseKey}&request[hardware-id]={hardwareId}";

                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseBody);

                    if (result.code == "ok")
                    {
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error : {ex.Message}");
                    return false;
                }
            }
        }

        private void ShowInputGrid()
        {
            InputGrid.Visibility = Visibility.Visible;
            CheckingGrid.Visibility = Visibility.Collapsed;
        }

        private bool LoadActivationStatus()
        {
            return DatabaseConnection.LoadActivationStatus();
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async void ActivateLicenseButton_Click(object sender, RoutedEventArgs e)
        {
            bool activationSuccess = await ActivateLicenseAsync();

            if (activationSuccess)
            {
                MessageBox.Show("License activated successfully!");

                this.Close();
                OpenMainWindow();
            }
            else
            {
                MessageBox.Show("License activation failed.");
            }
        }

        private async Task<bool> ActivateLicenseAsync()
        {
            return true;
        }

        private void OpenMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private async Task<bool> CheckLicenseAsync(string licenseKey, int retryCount = 3)
        {
            using (HttpClient client = new HttpClient())
            {
                for (int attempt = 0; attempt < retryCount; attempt++)
                {
                    try
                    {
                        string url = $"https://member.jvpartner.id/softsale/api/check-license?key={licenseKey}";

                        HttpResponseMessage response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        string responseBody = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"License check response: {responseBody}");

                        dynamic result = JsonConvert.DeserializeObject(responseBody);

                        if (result.code == "ok")
                        {
                            Debug.WriteLine($"License valid until: {result.license_expires}");
                            return true;
                        }
                        else if (result.code == "license_expired")
                        {
                            MessageBox.Show("License expired. Please renew your license.", "License Check", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else if (result.code == "license_not_found")
                        {
                            MessageBox.Show("License Key not found. Please check your license key.", "License Check", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return false;
                    }
                    catch (HttpRequestException ex) when (ex.Message.Contains("No such host is known"))
                    {
                        MessageBox.Show("Internet connection error. Please check your connection and try again.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        if (attempt == retryCount - 1)
                        {
                            MessageBox.Show("Internet connection error. Close this app and try again.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            Debug.WriteLine("Retrying license check due to internet error...");
                            await Task.Delay(2000); 
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error checking license: {ex.Message}", "License Check", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                return false;
            }
        }
    }
}
