using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace SqliteLogger
{
    public sealed class SqliteLoggerProvider : ILoggerProvider
    {
        private readonly IDisposable _onChangeToken;
        private SqliteLoggerConfiguration _currentConfig;
        private readonly ConcurrentDictionary<string, SqliteLogger> _loggers = new();

        public SqliteLoggerProvider(IOptionsMonitor<SqliteLoggerConfiguration> config)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
        }

        ILogger ILoggerProvider.CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new SqliteLogger(name, _currentConfig));
        }

        void IDisposable.Dispose()
        {
            foreach (SqliteLogger logger in _loggers.Values)
            {
                logger.Dispose();
            }

            _loggers.Clear();
            _onChangeToken.Dispose();
        }
    }
}
