using System;
using System.Collections.Generic;
using System.Data.SQLite;
using TELEBLASTER_PRO.Helpers;

namespace TELEBLASTER_PRO.Models
{
    internal class JoinedGroups
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public int TotalMember { get; set; }

        public static List<JoinedGroups> LoadJoinedGroups()
        {
            var joinedGroups = new List<JoinedGroups>();
            string query = "SELECT id, user_id, group_id, group_name, total_member FROM joined_groups";
            
            var connection = DatabaseConnection.Instance;
            using (var command = new SQLiteCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        joinedGroups.Add(new JoinedGroups
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            GroupId = reader.GetString(2),
                            GroupName = reader.GetString(3),
                            TotalMember = reader.GetInt32(4)
                        });
                    }
                }
            }
            return joinedGroups;
        }

        public static void SaveOrUpdateGroup(int userId, string groupId, string groupName, int totalMember)
        {
            string query = "INSERT INTO joined_groups (user_id, group_id, group_name, total_member) " +
                           "VALUES (@userId, @groupId, @groupName, @totalMember) " +
                           "ON CONFLICT(user_id, group_id) DO UPDATE SET " +
                           "group_name = @groupName, total_member = @totalMember";

            using (var connection = DatabaseConnection.Instance)
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@groupId", groupId);
                command.Parameters.AddWithValue("@groupName", groupName);
                command.Parameters.AddWithValue("@totalMember", totalMember);
                command.ExecuteNonQuery();
            }
        }
    }
}
