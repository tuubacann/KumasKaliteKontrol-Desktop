using Microsoft.Data.Sqlite;

namespace KumasKaliteKontrol.Data
{
    public static class DbContext
    {
        private static readonly string _connectionString =
            "Data Source=kumas.db";

        public static SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public static void CreateTables()
        {
            using var con = CreateConnection();
            con.Open();

            var cmd = con.CreateCommand();

            cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Fabrics (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,
                Code TEXT,
                TotalMeters INTEGER
            );

            CREATE TABLE IF NOT EXISTS Defects (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FabricId INTEGER,
                StartMeter INTEGER,
                EndMeter INTEGER,
                PointType INTEGER,
                Length INTEGER
            );

            CREATE TABLE IF NOT EXISTS Parties (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FabricId INTEGER,
                StartMeter INTEGER,
                EndMeter INTEGER,
                Length INTEGER,
                TotalPoints INTEGER,
                Quality INTEGER
            );
            ";

            cmd.ExecuteNonQuery();
        }
    }
}
