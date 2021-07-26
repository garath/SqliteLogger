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
            connection = new SqliteConnection(_connectionString);
            connection.Open();

            CreateTables(connection);

            if (_config.UseQueue)
            {
                connection.Close();

                _connectionString = "Data Source=file::memory:?cache=shared";
                connection = new SqliteConnection(_connectionString);
                connection.Open();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = $"ATTACH DATABASE '{fullFilePath}' AS 'file'";
                command.ExecuteNonQuery();

                CreateTables(connection);
                CreateTables(connection, "file");

                _queueConnection = connection;
                _queueTask = new LogQueueTask(_queueConnection)
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

    internal interface ILoggerConnection : IDisposable
    {
        public void Log(DateTimeOffset timestamp, string name, string level, string state, string message);
    }

    internal sealed class SqliteLoggerConnection : ILoggerConnection
    {
        private readonly SqliteConnection _connection;

        public SqliteLoggerConnection(SqliteConnection connection)
        {
            _connection = connection;
            _connection.Open();
        }

        public void Log(DateTimeOffset timestamp, string name, string level, string state, string message)
        {
            using SqliteCommand command = _connection.CreateCommand();
            command.CommandText =
                "INSERT INTO main.traces (" +
                        "timestamp, name, level, state, message" +
                    ") VALUES (" +
                        "@timestamp, @name, @level, @state, @message" +
                    ");";

            command.Parameters.AddWithValue("@timestamp", timestamp.ToString("O"));
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@level", level);
            command.Parameters.AddWithValue("@state", state);
            command.Parameters.AddWithValue("@message", message);

            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection.Close();
        }
    }
}
