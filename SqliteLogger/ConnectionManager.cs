using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SqliteLogger
{
    internal sealed class ConnectionManager : IDisposable
    {
        private readonly string _connectionString;
        private Task? _queueTask;
        private CancellationTokenSource _stoppingTokenSource = new();

        public ConnectionManager(SqliteLoggerConfiguration _config)
        {
            string fullFilePath = Path.GetFullPath(_config.FilePath);
            SqliteConnection connection;

            SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new()
            {
                DataSource = fullFilePath
            };
            connection = new SqliteConnection(sqliteConnectionStringBuilder.ToString());
            connection.Open();

            CreateTables(connection);

            if (_config.UseQueue)
            {
                connection.Close();

                connection = new SqliteConnection("Data Source=file::memory:?cache=shared");
                connection.Open();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = $"ATTACH DATABASE '{fullFilePath}' AS 'file'";
                command.ExecuteNonQuery();

                CreateTables(connection);
                CreateTables(connection, "file");

                _queueTask = new LogQueueTask(connection)
                    .RunAsync(_stoppingTokenSource.Token);
            }
        }

        private static void CreateTables(SqliteConnection connection, string schema = "main")
        {
            string tracesQuery =
                $"CREATE TABLE IF NOT EXISTS {schema}.traces (" +
                    "timestamp TEXT NOT NULL, " +
                    "name TEXT NOT NULL, " +
                    "level TEXT NOT NULL, " +
                    "state TEXT NULL, " +
                    "message TEXT" +
                ");";

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = tracesQuery;
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _stoppingTokenSource?.Cancel();
            _queueTask?.Wait();
        }
    }
}
