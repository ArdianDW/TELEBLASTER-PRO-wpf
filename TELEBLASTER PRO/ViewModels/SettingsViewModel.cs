using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using LicenseModel = TELEBLASTER_PRO.Models.License;
using TELEBLASTER_PRO.Helpers;
using System.Net.Http;
using System.Windows;
using System.Security.Cryptography;
using System.Management;
using Newtonsoft.Json;
using System.Windows.Input;
using TELEBLASTER_PRO.Views;

namespace TELEBLASTER_PRO.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private LicenseModel _license;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Email
        {
            get => _license.Email;
            set
            {
                if (_license.Email != value)
                {
                    _license.Email = value;
                    OnPropertyChanged(nameof(Email));
                }
            }
        }

        public string LicenseKey
        {
            get => _license.LicenseKey;
            set
            {
                if (_license.LicenseKey != value)
                {
                    _license.LicenseKey = value;
                    OnPropertyChanged(nameof(LicenseKey));
                }
            }
        }

        public string LicenseExpires
        {
            get => _license.LicenseExpires;
            set
            {
                if (_license.LicenseExpires != value)
                {
                    _license.LicenseExpires = value;
                    OnPropertyChanged(nameof(LicenseExpires));
                }
            }
        }

        public int Status
        {
            get => _license.Status;
            set
            {
                if (_license.Status != value)
                {
                    _license.Status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public ICommand DeactivateCommand { get; }

        public SettingsViewModel()
        {
            LoadLicenseData();
            DeactivateCommand = new RelayCommand(async _ => await DeactivateLicenseAsync());
        }

        private void LoadLicenseData()
        {
            // Load data from the database
            string email = DatabaseConnection.GetEmailFromDatabase();
            string licenseKey = DatabaseConnection.GetLicenseKeyFromDatabase();
            string licenseExpires = DatabaseConnection.GetLicenseExpiresFromDatabase();
            int status = DatabaseConnection.LoadActivationStatus() ? 1 : 0;

            _license = new LicenseModel(email, licenseKey, licenseExpires, status);
        }

        public async Task<bool> DeactivateLicenseAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = $"https://member.jvpartner.id/softsale/api/deactivate?key={LicenseKey}&request[hardware-id]={GetHardwareId()}";
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseBody);

                    if (result.code == "ok")
                    {
                        DatabaseConnection.UpdateLicenseStatus(0); // Update status di database
                        
                        // Tampilkan pesan dan tutup aplikasi
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("License has been successfully deactivated");
                            Application.Current.Shutdown(); // Tutup aplikasi sepenuhnya
                        });

                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deactivating license: {ex.Message}");
                    return false;
                }
            }
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
                MessageBox.Show($"Error getting CPU ID: {ex.Message}");
            }
            return null;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
