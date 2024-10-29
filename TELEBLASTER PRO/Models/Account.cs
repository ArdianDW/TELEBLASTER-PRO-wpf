using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace TELEBLASTER_PRO.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string SessionName { get; set; }
        public string TelegramUserId { get; set; }
        public string Username { get; set; }
        public string Realname { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }

        public Account(int id, string sessionName, string telegramUserId, string username, string realname, string phone, string status)
        {
            Id = id;
            SessionName = sessionName;
            TelegramUserId = telegramUserId;
            Username = username;
            Realname = realname;
            Phone = phone;
            Status = status;
        }

        public static List<Account> GetAccountsFromDatabase()
        {
            var accounts = new List<Account>();

            using (var connection = new SQLiteConnection("Data Source=teleblaster.db;Version=3;"))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT id, session_name, telegram_user_id, username, realname, phone_number, status FROM user_sessions", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string sessionName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                        string telegramUserId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                        string username = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                        string realname = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                        string phone = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                        string status = reader.IsDBNull(6) ? "Inactive" : (reader.GetInt32(6) == 1 ? "Active" : "Inactive");

                        accounts.Add(new Account(id, sessionName, telegramUserId, username, realname, phone, status));
                    }
                }
            }

            return accounts;
        }

        public void UpdateStatusInDatabase()
        {
            using (var connection = new SQLiteConnection("Data Source=teleblaster.db;Version=3;"))
            {
                connection.Open();
                var command = new SQLiteCommand("UPDATE user_sessions SET status = @status WHERE id = @id", connection);
                command.Parameters.AddWithValue("@status", Status == "Active" ? 1 : 0);
                command.Parameters.AddWithValue("@id", Id);
                command.ExecuteNonQuery();
            }
        }
    }
}
