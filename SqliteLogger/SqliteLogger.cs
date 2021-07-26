using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SqliteLogger
{
    public sealed class SqliteLogger : ILogger, IDisposable
    {
        private readonly string _name;
        private readonly ILoggerConnection _connection;

        internal IExternalScopeProvider ScopeProvider { get; set; } = NullScopeProvider.Instance;

        internal SqliteLogger(string name, ILoggerConnection connection)
        {
            _name = name;
            _connection = connection;
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

            _connection.Log(DateTimeOffset.UtcNow, _name, logLevel.ToString(), serializedScopes, formatter.Invoke(state, exception));
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
