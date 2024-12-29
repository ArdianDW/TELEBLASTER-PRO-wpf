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
using System.IO;
using ClosedXML.Excel;
using System.Linq;
using Microsoft.Win32;

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
        public ICommand ExportRawNumbersCommand { get; }
        public ICommand ImportNumbersCommand { get; }
        public ICommand ExportValidNumbersCommand { get; }

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
            ExportRawNumbersCommand = new RelayCommand(_ => ExportRawNumbersToExcel());
            ImportNumbersCommand = new RelayCommand(_ => ImportNumbersFromExcel());
            ExportValidNumbersCommand = new RelayCommand(_ => ExportValidNumbersToExcel());
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
                                dynamic functions = Py.Import("Backend.functions");
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
                    Debug.WriteLine($"Error threading: {pe.message}")
                    break;
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

        private void ExportRawNumbersToExcel()
        {
            try
            {
                var rawNumbers = GeneratedNumbers.Where(n => n.Status == null || n.Status == string.Empty).ToList();

                if (rawNumbers.Count == 0)
                {
                    MessageBox.Show("No raw numbers to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Raw Numbers");
                    worksheet.Cell(1, 1).Value = "Name";
                    worksheet.Cell(1, 2).Value = "Phone Number";
                    worksheet.Cell(1, 3).Value = "Username";
                    worksheet.Cell(1, 4).Value = "User ID";
                    worksheet.Cell(1, 5).Value = "Access Hash";
                    worksheet.Cell(1, 6).Value = "Status";

                    for (int i = 0; i < rawNumbers.Count; i++)
                    {
                        var number = rawNumbers[i];
                        worksheet.Cell(i + 2, 1).Value = number.PrefixName;
                        worksheet.Cell(i + 2, 2).Value = number.PhoneNumber;
                        worksheet.Cell(i + 2, 3).Value = number.Username;
                        worksheet.Cell(i + 2, 4).Value = number.UserId;
                        worksheet.Cell(i + 2, 5).Value = number.AccessHash;
                        worksheet.Cell(i + 2, 6).Value = number.Status;
                    }

                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Excel Workbook|*.xlsx",
                        Title = "Save Raw Numbers",
                        FileName = ""
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        workbook.SaveAs(saveFileDialog.FileName);
                        MessageBox.Show("Numbers exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while exporting numbers: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportNumbersFromExcel()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Workbook|*.xlsx",
                    Title = "Select a File to Import"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook(openFileDialog.FileName))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RowsUsed().Skip(1); // Skip header row

                        foreach (var row in rows)
                        {
                            var number = new NumberGenerated
                            {
                                PrefixName = row.Cell(1).GetValue<string>(),
                                PhoneNumber = row.Cell(2).GetValue<string>(),
                                Username = row.Cell(3).GetValue<string>(),
                                UserId = row.Cell(4).GetValue<string>(),
                                AccessHash = row.Cell(5).GetValue<string>(),
                                Status = row.Cell(6).GetValue<string>()
                            };

                            GeneratedNumbers.Add(number);
                        }
                    }

                    MessageBox.Show("Numbers imported successfully.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while importing numbers: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportValidNumbersToExcel()
        {
            try
            {
                var validNumbers = GeneratedNumbers.Where(n => n.Status == "Valid").ToList();

                if (validNumbers.Count == 0)
                {
                    MessageBox.Show("No valid numbers to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Valid Numbers");
                    worksheet.Cell(1, 1).Value = "No";
                    worksheet.Cell(1, 2).Value = "User ID";
                    worksheet.Cell(1, 3).Value = "Access Hash";
                    worksheet.Cell(1, 4).Value = "First Name";
                    worksheet.Cell(1, 5).Value = "Last Name";
                    worksheet.Cell(1, 6).Value = "Username";

                    for (int i = 0; i < validNumbers.Count; i++)
                    {
                        var number = validNumbers[i];
                        worksheet.Cell(i + 2, 1).Value = i + 1; // Nomor urut
                        worksheet.Cell(i + 2, 2).Value = number.UserId;
                        worksheet.Cell(i + 2, 3).Value = number.AccessHash;
                        worksheet.Cell(i + 2, 4).Value = number.PrefixName; // Assuming PrefixName is used as First Name
                        worksheet.Cell(i + 2, 5).Value = ""; // Last Name is empty
                        worksheet.Cell(i + 2, 6).Value = number.Username;
                    }

                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Excel Workbook|*.xlsx",
                        Title = "Save Valid Numbers",
                        FileName = "ValidNumbers.xlsx"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        workbook.SaveAs(saveFileDialog.FileName);
                        MessageBox.Show("Valid numbers exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while exporting valid numbers: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}