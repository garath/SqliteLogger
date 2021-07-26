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
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    $"CREATE TABLE IF NOT EXISTS {schema}.traces (" +
                        "timestamp TEXT NOT NULL, " +
                        "name TEXT NOT NULL, " +
                        "level TEXT NOT NULL, " +
                        "state TEXT NULL, " +
                        "exceptionid TEXT NULL, " +
                        "message TEXT" +
                    ");";

                command.ExecuteNonQuery();
            }



            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    $"CREATE TABLE IF NOT EXISTS {schema}.exceptions (" +
                        "timestamp TEXT NOT NULL, " + // denorming
                        "sequence INTEGER NOT NULL, " + // denorming
                        "id TEXT NOT NULL, " + // A primary key
                        "data TEXT NULL, " + // JSON IDictionary
                        "hresult INTEGER NULL, " +
                        "innerexceptionid INTEGER NULL, " + // primary key of another exception, creating a hierarchy? 
                        "message TEXT NOT NULL, " +
                        "source TEXT NULL, " +
                        "stacktrace TEXT NULL, " + // This can get big
                        "targetsite TEXT NULL" +
                    ");";

                command.ExecuteNonQuery();
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

    internal interface ILoggerConnection : IDisposable
    {
        public void Log(DateTimeOffset timestamp, string name, string level, string state, string? exceptionId, string message);
        public void LogException(string timestamp, int sequence, string id, string? data, int? hresult, string? innerexceptionid, string message, string? source, string? stacktrace, string? targetsite);
    }

    internal sealed class SqliteLoggerConnection : ILoggerConnection
    {
        private readonly SqliteConnection _connection;

        public SqliteLoggerConnection(SqliteConnection connection)
        {
            _connection = connection;
            _connection.Open();
        }

        public void Log(DateTimeOffset timestamp, string name, string level, string state, string? exceptionId, string message)
        {
            using SqliteCommand command = _connection.CreateCommand();
            command.CommandText =
                "INSERT INTO main.traces (" +
                        "timestamp, name, level, state, exceptionId, message" +
                    ") VALUES (" +
                        "@timestamp, @name, @level, @state, @exceptionId, @message" +
                    ");";

            command.Parameters.AddWithValue("@timestamp", timestamp.ToString("O"));
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@level", level);
            command.Parameters.AddWithValue("@state", state);
            command.Parameters.AddWithValue("@exceptionId", exceptionId ?? DBNull.Value.ToString());
            command.Parameters.AddWithValue("@message", message);

            command.ExecuteNonQuery();
        }

        public void LogException(string timestamp, int sequence, string id, string? data, int? hresult, string? innerexceptionid, string message, string? source, string? stacktrace, string? targetsite)
        {
            using SqliteCommand command = _connection.CreateCommand();
            command.CommandText =
                "INSERT INTO main.exceptions (" +
                    "timestamp, sequence, id, data, hresult, innerexceptionid, message, source , stacktrace, targetsite" +
                ") VALUES (" +
                    "@timestamp, @sequence, @id, @data, @hresult, @innerexceptionid, @message, @source , @stacktrace, @targetsite" +
                ");";

            command.Parameters.AddWithValue("@timestamp", timestamp);
            command.Parameters.AddWithValue("@sequence", sequence);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@data", data ?? DBNull.Value.ToString());
            command.Parameters.AddWithValue("@hresult", hresult == null ? DBNull.Value : hresult);
            command.Parameters.AddWithValue("@innerexceptionid", innerexceptionid == null ? DBNull.Value : innerexceptionid);
            command.Parameters.AddWithValue("@message", message);
            command.Parameters.AddWithValue("@source", source == null ? DBNull.Value : source);
            command.Parameters.AddWithValue("@stacktrace", stacktrace == null ? DBNull.Value : stacktrace);
            command.Parameters.AddWithValue("@targetsite", targetsite == null ? DBNull.Value : targetsite);

            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection.Close();
        }
    }
}
