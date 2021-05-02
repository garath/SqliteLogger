using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;

namespace SqliteLogger
{
    public sealed class SqliteLogger : ILogger
    {
        private readonly string _name;
        private readonly SqliteLoggerConfiguration _config;
        private readonly SqliteConnection connection;

        public SqliteLogger(string name, SqliteLoggerConfiguration config)
        {
            _name = name;
            _config = config;

            connection = new SqliteConnection(_config.ConnectionString);
            connection.Open();

            CreateTables();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return default;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO traces (" +
                        "timestamp, name, level, state, message" +
                    ") VALUES (" +
                        "@timestamp, @name, @level, @state, @message" +
                    ");";

            command.Parameters.AddWithValue("@timestamp", DateTimeOffset.UtcNow.ToString("O"));
            command.Parameters.AddWithValue("@name", _name);
            command.Parameters.AddWithValue("@level", logLevel.ToString());
            command.Parameters.AddWithValue("@state", state.ToString());
            command.Parameters.AddWithValue("@message", formatter.Invoke(state, exception));

            command.ExecuteNonQuery();
        }

        private void CreateTables()
        {
            string tracesQuery =
                "CREATE TABLE IF NOT EXISTS traces (" +
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
            connection?.Close();
        }
    }
}
