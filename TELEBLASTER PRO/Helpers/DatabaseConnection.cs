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
    }
}
