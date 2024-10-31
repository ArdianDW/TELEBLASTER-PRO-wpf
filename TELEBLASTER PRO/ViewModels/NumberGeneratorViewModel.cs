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
        private string _prefixName;
        private string _numberPrefix;
        private int _addValue;

        public string PrefixName
        {
            get => _prefixName;
            set
            {
                _prefixName = value;
                OnPropertyChanged();
            }
        }

        public string NumberPrefix
        {
            get => _numberPrefix;
            set
            {
                _numberPrefix = value;
                OnPropertyChanged();
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

        public ObservableCollection<NumberGenerated> GeneratedNumbers { get; set; }

        public ICommand GenerateCommand { get; }

        public NumberGeneratorViewModel()
        {
            GeneratedNumbers = new ObservableCollection<NumberGenerated>();
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
