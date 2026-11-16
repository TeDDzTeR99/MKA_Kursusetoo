using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace MKA_KursuseToo
{
    // All database access goes through this class, so the path and connection code live in one place
    internal static class WeaponDatabase
    {
        // The database file is copied next to the .exe when the project is built
        public static string DbPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "elden_ring_weapons.db");

        public static bool Exists => File.Exists(DbPath);

        // The app only reads data, so the database is opened read-only. This also means a missing
        // file gives an error instead of SQLite silently creating a new, empty database.
        // The builder takes care of quoting, so a path with special characters (e.g. ';') still works.
        private static string ConnectionString => new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        // Runs a query and returns the result as a DataTable.
        // Parameters are passed separately (e.g. { "@id", 5 }) so user input can't change the SQL.
        public static DataTable Query(string sql, Dictionary<string, object> parameters = null)
        {
            using (var con = new SqliteConnection(ConnectionString))
            {
                con.Open();

                using (var cmd = new SqliteCommand(sql, con))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                            cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        var dt = new DataTable();
                        dt.Load(reader);
                        return dt;
                    }
                }
            }
        }
    }
}
