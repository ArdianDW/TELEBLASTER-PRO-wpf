using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using TELEBLASTER_PRO.Helpers;

namespace TELEBLASTER_PRO.Models
{
    internal class UserChats
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ChatUserId { get; set; }
        public string AccessHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }

        public static List<UserChats> LoadUserChats(int extractorUserId)
        {
            var userChats = new List<UserChats>();
            string query = "SELECT id, user_id, chat_user_id, access_hash, first_name, last_name, username FROM user_chats WHERE user_id = @userId";
            
            var connection = DatabaseConnection.Instance;
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", extractorUserId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        userChats.Add(new UserChats
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            ChatUserId = reader.GetString(2),
                            AccessHash = reader.GetString(3),
                            FirstName = reader.GetString(4),
                            LastName = reader.GetString(5),
                            UserName = reader.GetString(6)
                        });
                    }
                }
            }
            return userChats;
        }
    }
}
