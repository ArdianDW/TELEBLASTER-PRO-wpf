using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.ComponentModel;
using System.Diagnostics;
using TELEBLASTER_PRO.Helpers;

namespace TELEBLASTER_PRO.Models
{
    public class GroupLinks : INotifyPropertyChanged
    {
        private int _check;
        public int Id { get; set; }
        public string Link { get; set; }
        public int Check
        {
            get => _check;
            set
            {
                if (_check != value)
                {
                    _check = value;
                    Debug.WriteLine($"Check property changed for ID {Id}: {_check}");
                    OnPropertyChanged();
                }
            }
        }
        public string GroupName { get; set; }
        public int TotalMember { get; set; }
        private string _type;
        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Status { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static List<GroupLinks> LoadGroupLinks()
        {
            var groupLinks = new List<GroupLinks>();
            string query = "SELECT id, link, \"check\", group_name, total_member, type, status FROM group_links";

            var connection = DatabaseConnection.Instance;
            using (var command = new SQLiteCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        groupLinks.Add(new GroupLinks
                        {
                            Id = reader.GetInt32(0),
                            Link = reader.GetString(1),
                            Check = reader.GetInt32(2),
                            GroupName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            TotalMember = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                            Type = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            Status = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
                        });
                    }
                }
            }
            return groupLinks;
        }

        // Method to update the Check status in the database
        public void UpdateCheckStatusInDatabase()
        {
            string query = "UPDATE group_links SET \"check\" = @check WHERE id = @id";
            var connection = DatabaseConnection.Instance;
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@check", Check);
                command.Parameters.AddWithValue("@id", Id);
                command.ExecuteNonQuery();
            }
        }
    }
}
