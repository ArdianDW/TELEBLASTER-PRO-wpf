using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TELEBLASTER_PRO.Models;

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

        public NumberGeneratorViewModel()
        {
            GenerateCommand = new RelayCommand(_ => GenerateNumbers());
        }

        private void GenerateNumbers()
        {
            GeneratedNumbers.Clear();
            for (int i = 1; i <= AddValue; i++)
            {
                var newNumber = new NumberGenerated
                {
                    PrefixName = $"{PrefixName}{i}",
                    PhoneNumber = $"{NumberPrefix}{i:D4}"
                };

                // Simpan ke database
                SaveNumberToDatabase(newNumber);

                // Tambahkan ke koleksi
                GeneratedNumbers.Add(newNumber);
            }
        }

        private void SaveNumberToDatabase(NumberGenerated number)
        {
            using (var connection = new SQLiteConnection("Data Source=teleblaster.db"))
            {
                connection.Open();
                string query = "INSERT INTO number_generated (prefix_name, phone_number) VALUES (@prefixName, @phoneNumber)";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@prefixName", number.PrefixName);
                    command.Parameters.AddWithValue("@phoneNumber", number.PhoneNumber);
                    command.ExecuteNonQuery();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
