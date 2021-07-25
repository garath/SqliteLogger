using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SqliteLogger
{
    public sealed class SqliteLogger : ILogger, IDisposable
    {
        private readonly string _name;
        private readonly SqliteLoggerConfiguration _config;
        private readonly SqliteConnection connection;

        private Task? _queueTask;
        private CancellationTokenSource _stoppingTokenSource = new ();

        internal IExternalScopeProvider ScopeProvider { get; set; } = new NullScopeProvider();

        public SqliteLogger(string name, SqliteLoggerConfiguration config)
        {
            _name = name;
            _config = config;

            string fullFilePath = Path.GetFullPath(_config.FilePath);

            SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new()
            {
                DataSource = fullFilePath
            };
            connection = new SqliteConnection(sqliteConnectionStringBuilder.ToString());
            connection.Open();

            CreateTables();

            if (_config.UseQueue)
            {
                connection.Close();

                connection = new SqliteConnection("Data Source=file::memory:?cache=shared");
                connection.Open();

                SqliteCommand command = connection.CreateCommand();
                command.CommandText = $"ATTACH DATABASE '{fullFilePath}' AS 'file'";
                command.ExecuteNonQuery();

                CreateTables();
                CreateTables("file");

                _queueTask = new LogQueueTask(connection)
                    .RunAsync(_stoppingTokenSource.Token);
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return ScopeProvider.Push(state);
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

            Dictionary<string, object?> scopes = new();
            List<string> unnamedScopes = new();

            var stateCollection = state as IReadOnlyCollection<KeyValuePair<string, object?>> 
                ?? Array.Empty<KeyValuePair<string, object?>>();
            foreach (var stateItem in stateCollection)
            {
                scopes.Add(stateItem.Key, stateItem.Value);
            }

            ScopeProvider.ForEachScope((scope, state) =>
            {
                if (scope is KeyValuePair<string, object?> kvp)
                {
                    scopes.Add(kvp.Key, kvp.Value);
                }
                else if (scope is IReadOnlyCollection<KeyValuePair<string, object?>> scopeList)
                {
                    foreach((string scopeKey, object? scopeValue) in scopeList)
                    {
                        scopes.Add(scopeKey, scopeValue);
                    }
                }
                else
                {
                    string? scopeString = scope.ToString();
                    if (scopeString is not null)
                    {
                        unnamedScopes.Add(scopeString);
                    }
                }
            }, scopes);

            if (unnamedScopes.Count > 0)
            {
                scopes.Add("Scope", unnamedScopes);
            }

            string serializedScopes = JsonSerializer.Serialize(scopes);

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO main.traces (" +
                        "timestamp, name, level, state, message" +
                    ") VALUES (" +
                        "@timestamp, @name, @level, @state, @message" +
                    ");";

            command.Parameters.AddWithValue("@timestamp", DateTimeOffset.UtcNow.ToString("O"));
            command.Parameters.AddWithValue("@name", _name);
            command.Parameters.AddWithValue("@level", logLevel.ToString());
            command.Parameters.AddWithValue("@state", serializedScopes);
            command.Parameters.AddWithValue("@message", formatter.Invoke(state, exception));

            command.ExecuteNonQuery();
        }

        private void CreateTables(string schema = "main")
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
            connection?.Close();
        }
    }
}
