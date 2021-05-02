using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;

namespace SqliteLogger
{
    public static class SqliteLoggerExtensions
    {
        public static ILoggingBuilder AddSqliteLogger(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, SqliteLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions<SqliteLoggerConfiguration, SqliteLoggerProvider>(builder.Services);

            return builder;
        }

        public static ILoggingBuilder AddSqliteLogger(this ILoggingBuilder builder, Action<SqliteLoggerConfiguration> configure)
        {
            builder.AddSqliteLogger();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
