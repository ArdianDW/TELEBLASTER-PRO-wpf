using System;
using System.Data.SQLite; // Pastikan Anda memiliki referensi ke SQLite

namespace TELEBLASTER_PRO.Models
{
    public class NumberGenerated
    {
        public int Id { get; set; }
        public string PrefixName { get; set; }
        public string PhoneNumber { get; set; }
        public string UserId { get; set; }
        public string AccessHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Status { get; set; }

        public static NumberGenerated LoadNumber(int id, string connectionString)
        {
            NumberGenerated numberGenerated = null;

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
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
            }

            return numberGenerated;
        }
    }
}
