using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TELEBLASTER_PRO.Models;
using Python.Runtime;
using System.Diagnostics;
using TELEBLASTER_PRO.Helpers;
using System.Windows;

namespace TELEBLASTER_PRO.ViewModels
{
    internal class NumberGeneratorViewModel : INotifyPropertyChanged
    {
        private int _addValue;
        private bool _isLoading;
        private int _totalNumbers;
        private int _validNumbers;
        private bool _isValidating;
        private bool _stopValidation;

        public string PrefixName
        {
            get => ExtractedDataStore.Instance.PrefixName;
            set
            {
                if (ExtractedDataStore.Instance.PrefixName != value)
                {
                    ExtractedDataStore.Instance.PrefixName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string NumberPrefix
        {
            get => ExtractedDataStore.Instance.NumberPrefix;
            set
            {
                if (ExtractedDataStore.Instance.NumberPrefix != value)
                {
                    ExtractedDataStore.Instance.NumberPrefix = value;
                    OnPropertyChanged();
                }
            }
        }

        public int AddValue
        {
            get => _addValue;
            set
            {
                _addValue = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<NumberGenerated> GeneratedNumbers
        {
            get => ExtractedDataStore.Instance.GeneratedNumbers;
        }

        public ICommand GenerateCommand { get; }
        public ICommand ValidateCommand { get; }
        public ICommand ClearNumbersCommand { get; }
        public ICommand StopValidationCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public int TotalNumbers
        {
            get => _totalNumbers;
            set
            {
                _totalNumbers = value;
                OnPropertyChanged();
            }
        }

        public int ValidNumbers
        {
            get => _validNumbers;
            set
            {
                _validNumbers = value;
                OnPropertyChanged();
            }
        }

        public bool IsValidating
        {
            get => _isValidating;
            set
            {
                _isValidating = value;
                OnPropertyChanged();
            }
        }

        public bool StopValidation
        {
            get => _stopValidation;
            set
            {
                _stopValidation = value;
                OnPropertyChanged();
            }
        }

        public NumberGeneratorViewModel()
        {
            GenerateCommand = new RelayCommand(_ => GenerateNumbers());
            ValidateCommand = new RelayCommand(_ => Task.Run(() => ValidateNumbersAsync()));
            ClearNumbersCommand = new RelayCommand(_ => ClearNumbers());
            StopValidationCommand = new RelayCommand(_ => StopValidation = true, _ => IsValidating);
        }

        private void GenerateNumbers()
        {
            GeneratedNumbers.Clear();
            TotalNumbers = 0; // Reset total numbers
            if (long.TryParse(NumberPrefix, out long baseNumber))
            {
                for (int i = 1; i <= AddValue; i++)
                {
                    var newNumber = new NumberGenerated
                    {
                        PrefixName = $"{PrefixName}{i}",
                        PhoneNumber = (baseNumber + i).ToString()
                    };

                    SaveNumberToDatabase(newNumber);

                    GeneratedNumbers.Add(newNumber);
                }
            }
            else
            {
                throw new InvalidOperationException("NumberPrefix harus berupa angka.");
            }
            TotalNumbers = GeneratedNumbers.Count; // Update total numbers
        }

        private void SaveNumberToDatabase(NumberGenerated number)
        {
            var connection = DatabaseConnection.Instance;
                string query = "INSERT INTO number_generated (prefix_name, phone_number) VALUES (@prefixName, @phoneNumber)";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@prefixName", number.PrefixName);
                    command.Parameters.AddWithValue("@phoneNumber", number.PhoneNumber);
                    command.ExecuteNonQuery();
                }
        }

        private async Task ValidateNumbersAsync()
        {
            IsValidating = true;
            StopValidation = false;

            var activeAccounts = GetActiveAccounts().ToList();
            if (!activeAccounts.Any())
            {
                Debug.WriteLine("No active accounts available for validation.");
                IsValidating = false;
                return;
            }

            int accountIndex = 0;
            int numbersPerAccount = 1; // Kurangi jumlah nomor per batch
            var random = new Random();

            bool allNumbersValidated = false;
            while (!allNumbersValidated && !StopValidation)
            {
                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        IsLoading = true; // Menampilkan ValidateStatusBar
                    });

                    for (int i = 0; i < GeneratedNumbers.Count; i++)
                    {
                        if (StopValidation)
                        {
                            break;
                        }

                        var number = GeneratedNumbers[i];
                        string currentSession = activeAccounts[accountIndex].SessionName;

                        Debug.WriteLine($"Validating phone number: {number.PhoneNumber} using session: {currentSession}");

                        // Pindahkan operasi Python ke dalam Dispatcher.Invoke
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            using (Py.GIL())
                            {
                                dynamic functions = Py.Import("functions");
                                var result = functions.validate_phone_number_sync(currentSession, number.PhoneNumber);
                                bool isValid = result[0].As<bool>();
                                dynamic userInfo = result[1];

                                // Logging hasil dari Python
                                Debug.WriteLine($"Validation result for {number.PhoneNumber}: isValid={isValid}, userInfo={userInfo}");

                                if (isValid)
                                {
                                    number.UserId = userInfo["user_id"].As<long>().ToString();
                                    number.AccessHash = userInfo["access_hash"].As<long>().ToString();
                                    number.Username = userInfo["username"].As<string>();
                                    number.Status = "Valid";

                                    Debug.WriteLine($"Phone number {number.PhoneNumber} is valid. User ID: {number.UserId}, Username: {number.Username}");
                                }
                                else
                                {
                                    number.Status = "Invalid";
                                    number.UserId = string.Empty;
                                    number.AccessHash = string.Empty;
                                    number.Username = string.Empty;

                                    Debug.WriteLine($"Phone number {number.PhoneNumber} is not valid.");
                                }
                            }
                        });

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            OnPropertyChanged(nameof(GeneratedNumbers));
                        });

                        int delay = random.Next(1, 2) * 1000; // Tambahkan jeda yang lebih panjang
                        await Task.Delay(delay);

                        if ((i + 1) % numbersPerAccount == 0 && activeAccounts.Count > 1)
                        {
                            accountIndex = (accountIndex + 1) % activeAccounts.Count;
                            await Task.Delay(5000); // Tambahkan jeda setelah setiap batch
                        }
                    }

                    allNumbersValidated = true; // Semua nomor berhasil divalidasi
                }
                catch (PythonException pe)
                {
                    if (pe.Message.Contains("set_wakeup_fd only works in main thread of the main interpreter"))
                    {
                        Debug.WriteLine("Retrying due to Python error: " + pe.Message);
                        await Task.Delay(1000); // Wait before retrying
                        continue; // Retry the validation process
                    }
                    else
                    {
                        Debug.WriteLine($"Python error during number validation: {pe.Message}");
                        break; // Exit loop if it's a different Python error
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during number validation: {ex.Message}");
                    break; // Exit loop on other exceptions
                }
                finally
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        IsLoading = false; // Menyembunyikan ValidateStatusBar
                    });
                }
            }

            IsValidating = false;
            ValidNumbers = GeneratedNumbers.Count(n => n.Status == "Valid"); // Update valid numbers
        }

        private IEnumerable<Account> GetActiveAccounts()
        {
            return Account.GetAccountsFromDatabase().Where(account => account.Status == "Active");
        }

        private void ClearNumbers()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all generated numbers?",
                "Clear Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                GeneratedNumbers.Clear();
                TotalNumbers = 0; // Reset total numbers
                ValidNumbers = 0; // Reset valid numbers
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}