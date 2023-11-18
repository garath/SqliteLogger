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

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
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
                    foreach ((string scopeKey, object? scopeValue) in scopeList)
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

            List<(Guid Id, Exception Exception)> exceptionTree = new();
            Exception? nextException = exception;
            while (nextException != null)
            {
                Guid exceptionId = Guid.NewGuid();
                exceptionTree.Add((exceptionId, nextException));
                nextException = nextException.InnerException;
            }

            using var connectionScope = _connection.BeginScope();

            _connection.Log(
                timestamp: timestamp,
                name: _name,
                level: logLevel.ToString(),
                state: serializedScopes,
                exceptionId: exceptionTree.Count == 0 ? null : exceptionTree[0].Id.ToString(),
                message: formatter.Invoke(state, exception));

            bool firstException = true;
            for (int i = exceptionTree.Count - 1; i >= 0 ; i--)
            {
                (Guid Id, Exception Exception) = exceptionTree[i];

                string? serializedData = null;
                if (Exception.Data is not null)
                {
                    serializedData = JsonSerializer.Serialize(Exception.Data);
                }

                string? innerExceptionId = null;
                if (!firstException)
                {
                    innerExceptionId = exceptionTree[i + 1].Id.ToString();
                }

                _connection.LogException(
                    timestamp: timestamp.ToString("O"),
                    sequence: i,
                    id: Id.ToString(),
                    data: serializedData,
                    hResult: Exception.HResult,
                    innerExceptionId: innerExceptionId,
                    message: Exception.Message,
                    source: Exception.Source,
                    stackTrace: Exception.StackTrace,
                    targetSite: Exception.TargetSite?.Name);

                firstException = false;
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
