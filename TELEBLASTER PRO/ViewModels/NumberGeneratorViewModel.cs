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

namespace TELEBLASTER_PRO.ViewModels
{
    internal class NumberGeneratorViewModel : INotifyPropertyChanged
    {
        private int _addValue;

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

        public NumberGeneratorViewModel()
        {
            GenerateCommand = new RelayCommand(_ => GenerateNumbers());
            ValidateCommand = new RelayCommand(async _ => await ValidateNumbersAsync());
        }

        private void GenerateNumbers()
        {
            GeneratedNumbers.Clear();
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
            var activeAccounts = GetActiveAccounts().ToList();
            int accountIndex = 0;
            int numbersPerAccount = 5;
            var random = new Random();

            using (Py.GIL())
            {
                dynamic functions = Py.Import("functions");
                for (int i = 0; i < GeneratedNumbers.Count; i++)
                {
                    var number = GeneratedNumbers[i];
                    string currentSession = activeAccounts[accountIndex].SessionName; 

                    Debug.WriteLine($"Validating phone number: {number.PhoneNumber} using session: {currentSession}");

                    var result = functions.validate_phone_number(currentSession, number.PhoneNumber);
                    bool isValid = result[0].As<bool>();
                    if (isValid)
                    {
                        dynamic userInfo = result[1];
                        number.UserId = userInfo["user_id"].As<long>().ToString();
                        number.AccessHash = userInfo["access_hash"].As<long>().ToString();
                        number.Username = userInfo["username"].As<string>();
                        number.Status = "Valid";

                        Debug.WriteLine($"Phone number {number.PhoneNumber} is valid. User ID: {number.UserId}, Username: {number.Username}");
                    }
                    else
                    {
                        number.Status = "Not Valid";

                        Debug.WriteLine($"Phone number {number.PhoneNumber} is not valid.");
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        OnPropertyChanged(nameof(number.UserId));
                        OnPropertyChanged(nameof(number.AccessHash));
                        OnPropertyChanged(nameof(number.Username));
                        OnPropertyChanged(nameof(number.Status));
                    });

                    int delay = random.Next(1, 2) * 1000; 
                    await Task.Delay(delay);

                    if ((i + 1) % numbersPerAccount == 0)
                    {
                        accountIndex = (accountIndex + 1) % activeAccounts.Count;
                    }
                }
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(GeneratedNumbers));
            });
        }

        private IEnumerable<Account> GetActiveAccounts()
        {
            return Account.GetAccountsFromDatabase().Where(account => account.Status == "Active");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}