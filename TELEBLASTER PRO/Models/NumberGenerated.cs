using System;
using System.ComponentModel;
using System.Data.SQLite;
using TELEBLASTER_PRO.Helpers;

namespace TELEBLASTER_PRO.Models
{
    public class NumberGenerated : INotifyPropertyChanged
    {
        private string _prefixName;
        private string _phoneNumber;
        private string _userId;
        private string _accessHash;
        private string _firstName;
        private string _lastName;
        private string _username;
        private string _status;

        public int Id { get; set; }
        public string PrefixName
        {
            get => _prefixName;
            set
            {
                _prefixName = value;
                OnPropertyChanged(nameof(PrefixName));
            }
        }
        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                _phoneNumber = value;
                OnPropertyChanged(nameof(PhoneNumber));
            }
        }
        public string UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
            }
        }
        public string AccessHash
        {
            get => _accessHash;
            set
            {
                _accessHash = value;
                OnPropertyChanged(nameof(AccessHash));
            }
        }
        public string FirstName
        {
            get => _firstName;
            set
            {
                _firstName = value;
                OnPropertyChanged(nameof(FirstName));
            }
        }
        public string LastName
        {
            get => _lastName;
            set
            {
                _lastName = value;
                OnPropertyChanged(nameof(LastName));
            }
        }
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public static NumberGenerated LoadNumber(int id)
        {
            NumberGenerated numberGenerated = null;

            var connection = DatabaseConnection.Instance;
            string query = "SELECT * FROM number_generated WHERE id = @id";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        numberGenerated = new NumberGenerated
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            PrefixName = reader.GetString(reader.GetOrdinal("prefix_name")),
                            PhoneNumber = reader.GetString(reader.GetOrdinal("phone_number")),
                            UserId = reader.GetString(reader.GetOrdinal("user_id")),
                            AccessHash = reader.GetString(reader.GetOrdinal("access_hash")),
                            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                            LastName = reader.GetString(reader.GetOrdinal("last_name")),
                            Username = reader.GetString(reader.GetOrdinal("username")),
                            Status = reader.GetString(reader.GetOrdinal("status"))
                        };
                    }
                }
            }

            return numberGenerated;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
