using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SqliteLogger
{
    internal sealed class ConnectionManager : IDisposable
    {
        private readonly string _connectionString;
        private readonly CancellationTokenSource _stoppingTokenSource = new();

        private readonly SqliteConnection? _queueConnection;
        private readonly Task? _queueTask;

        public ConnectionManager(SqliteLoggerConfiguration _config)
        {
            string fullFilePath = Path.GetFullPath(_config.FilePath);
            SqliteConnection connection;

            SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new()
            {
                DataSource = fullFilePath
            };
            _connectionString = sqliteConnectionStringBuilder.ToString();

            // Ensure the file is created
            using (connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                CreateTables(connection);
            }

            if (_config.UseQueue)
            {
                _connectionString = "Data Source=file::memory:?cache=shared";
                connection = new SqliteConnection(_connectionString);
                connection.Open();

                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = $"ATTACH DATABASE '{fullFilePath}' AS 'file'";
                    command.ExecuteNonQuery();
                }

                CreateTables(connection);
                CreateTables(connection, "file");

                _queueConnection = connection;
                _queueTask = new LogQueueTask(_queueConnection)
                    .RunAsync(_stoppingTokenSource.Token);
            }
        }

        private static void CreateTables(SqliteConnection connection, string schema = "main")
        {
            using (var transaction = connection.BeginTransaction())
            {
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText =
                        $"CREATE TABLE IF NOT EXISTS {schema}.traces (" +
                            "timestamp TEXT NOT NULL, " +
                            "name TEXT NOT NULL, " +
                            "level TEXT NOT NULL, " +
                            "state TEXT, " +
                            "exception_id TEXT , " +
                            "message TEXT" +
                        ");";

                    command.ExecuteNonQuery();

                    command.CommandText =
                        $"CREATE TABLE IF NOT EXISTS {schema}.exceptions (" +
                            "timestamp TEXT NOT NULL, " + // denorming
                            "sequence INTEGER NOT NULL, " + // denorming
                            "id TEXT PRIMARY KEY, " + // A primary key
                            "data TEXT, " + // JSON IDictionary
                            "hresult INTEGER, " +
                            "inner_exception_id TEXT REFERENCES exceptions(id) ON DELETE CASCADE ON UPDATE CASCADE, " + // primary key of another exception, creating a hierarchy 
                            "message TEXT NOT NULL, " +
                            "source TEXT, " +
                            "stacktrace TEXT, " + // This can get big
                            "targetsite TEXT" +
                        ");";

                    command.ExecuteNonQuery();
                }
            }
        }

        public SqliteLoggerConnection CreateConnection()
        {
            return new SqliteLoggerConnection(new SqliteConnection(_connectionString));
        }

        public void Dispose()
        {
            _stoppingTokenSource?.Cancel();
            _queueTask?.Wait();
            _queueConnection?.Close();
        }
    }
}
