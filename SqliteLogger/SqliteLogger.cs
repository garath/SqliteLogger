using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SqliteLogger
{
    public sealed class SqliteLogger : ILogger, IDisposable
    {
        private readonly string _name;
        private readonly SqliteLoggerConfiguration _config;
        private readonly SqliteConnection connection;

        internal IExternalScopeProvider ScopeProvider { get; set; }

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
            return ScopeProvider?.Push(state) ?? default;
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
                "INSERT INTO traces (" +
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
