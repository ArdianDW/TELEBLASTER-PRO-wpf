using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace TELEBLASTER_PRO.Helpers
{
    public class DatabaseConnection
    {
        private static SQLiteConnection _instance;
        private static readonly object _lock = new object();

        private DatabaseConnection() { }

        public static SQLiteConnection Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            string dbPath = System.IO.Path.Combine(appDataPath, "TELEBLASTER_PRO", "teleblaster.db");
                            _instance = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                            _instance.Open();
                        }
                    }
                }
                return _instance;
            }
        }

        public static void SaveLicense(string email, string licenseKey, string expires)
        {
            using (var command = new SQLiteCommand(Instance))
            {
                command.CommandText = "DELETE FROM license";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO license (email, license_key, license_expires, status) VALUES (@Email, @LicenseKey, @Expires, 1)";
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@LicenseKey", licenseKey);
                command.Parameters.AddWithValue("@Expires", expires);
                command.ExecuteNonQuery();
            }
        }

        public static bool LoadActivationStatus()
        {
            using (var command = new SQLiteCommand("SELECT status FROM license LIMIT 1", Instance))
            {
                object result = command.ExecuteScalar();
                return result != null && Convert.ToInt32(result) == 1;
            }
        }

        public static string GetEmailFromDatabase()
        {
            using (var command = new SQLiteCommand("SELECT email FROM license LIMIT 1", Instance))
            {
                object result = command.ExecuteScalar();
                return result != null ? result.ToString() : string.Empty;
            }
        }

        public static string GetLicenseKeyFromDatabase()
        {
            using (var command = new SQLiteCommand("SELECT license_key FROM license LIMIT 1", Instance))
            {
                object result = command.ExecuteScalar();
                return result != null ? result.ToString() : string.Empty;
            }
        }

        public static string GetLicenseExpiresFromDatabase()
        {
            using (var command = new SQLiteCommand("SELECT license_expires FROM license LIMIT 1", Instance))
            {
                object result = command.ExecuteScalar();
                return result != null ? result.ToString() : string.Empty;
            }
        }

        public static void UpdateLicenseStatus(int status)
        {
            using (var command = new SQLiteCommand("UPDATE license SET status = @Status", Instance))
            {
                command.Parameters.AddWithValue("@Status", status);
                command.ExecuteNonQuery();
            }
        }
    }
}
