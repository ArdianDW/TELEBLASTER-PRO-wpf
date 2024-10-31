using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite; 

namespace TELEBLASTER_PRO.Models
{
    internal class Contacts
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ContactId { get; set; }
        public string AccessHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public bool IsChecked { get; set; }

        public static List<Contacts> LoadContacts(SQLiteConnection connection)
        {
            var contacts = new List<Contacts>();
            string query = "SELECT id, user_id, contact_id, access_hash, first_name, last_name, user_name FROM contacts";
            
            using (var command = new SQLiteCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        contacts.Add(new Contacts
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            ContactId = reader.GetString(2),
                            AccessHash = reader.GetString(3),
                            FirstName = reader.GetString(4),
                            LastName = reader.GetString(5),
                            UserName = reader.GetString(6),
                            IsChecked = false
                        });
                    }
                }
            }
            return contacts;
        }
    }
}