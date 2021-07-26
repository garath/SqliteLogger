using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace SqliteLogger
{
    public sealed class SqliteLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly ConcurrentDictionary<string, SqliteLogger> _loggers = new();
        private readonly SqliteLoggerConfiguration _config;
        private readonly ConnectionManager _connectionManager;
        private IExternalScopeProvider? _scopeProvider;        

        public SqliteLoggerProvider(IOptions<SqliteLoggerConfiguration> config)
        {
            _config = config.Value;
            _connectionManager = new ConnectionManager(_config);
        }

        ILogger ILoggerProvider.CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name =>
                new SqliteLogger(name, _connectionManager.CreateConnection())
                {
                    ScopeProvider = _scopeProvider ?? NullScopeProvider.Instance
                });
        }

        void IDisposable.Dispose()
        {
            foreach (SqliteLogger logger in _loggers.Values)
            {
                logger.Dispose();
            }

            _connectionManager.Dispose();
            _loggers.Clear();
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;

            foreach (SqliteLogger logger in _loggers.Values)
            {
                logger.ScopeProvider = _scopeProvider;
            }
        }
    }
}
