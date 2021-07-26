using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace SqliteLogger
{
    public sealed class SqliteLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly ConcurrentDictionary<string, SqliteLogger> _loggers = new();
        private readonly SqliteLoggerConfiguration _currentConfig;
        private IExternalScopeProvider? _scopeProvider;

        public SqliteLoggerProvider(IOptions<SqliteLoggerConfiguration> config)
        {
            _currentConfig = config.Value;
        }

        ILogger ILoggerProvider.CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name =>
                new SqliteLogger(name, _currentConfig)
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
