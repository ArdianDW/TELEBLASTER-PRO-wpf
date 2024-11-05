using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace TELEBLASTER_PRO.Models
{
    internal class GroupMembers
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string MemberId { get; set; }
        public string AccessHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }

        public static List<GroupMembers> LoadGroupMembers(SQLiteConnection connection, long groupId)
        {
            var groupMembers = new List<GroupMembers>();
            string query = "SELECT id, group_id, user_id, member_id, access_hash, first_name, last_name, username FROM group_members WHERE group_id = @GroupId";
            
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@GroupId", groupId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        groupMembers.Add(new GroupMembers
                        {
                            Id = reader.GetInt32(0),
                            GroupId = reader.GetInt32(1),
                            UserId = reader.GetInt32(2),
                            MemberId = reader.GetString(3),
                            AccessHash = reader.GetString(4),
                            FirstName = reader.GetString(5),
                            LastName = reader.GetString(6),
                            UserName = reader.GetString(7)
                        });
                    }
                }
            }
            return groupMembers;
        }
    }
}
