using Microsoft.Extensions.Hosting;
using System;
using SqliteLogger;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ConsoleStress
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(config =>
                {
                    config.ClearProviders();
                    config.SetMinimumLevel(LogLevel.Trace);
                    config.AddSqliteLogger(config =>
                    {
                        config.FilePath = "log.db";
                        config.UseQueue = true;
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<LoggingStressWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }

    internal class LoggingStressWorker : BackgroundService
    {
        private readonly ILogger<LoggingStressWorker> _logger;

        public LoggingStressWorker(ILogger<LoggingStressWorker> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                KeyValuePair<string, object?> smallScope = new("PropertyThing", 123);
                IReadOnlyList<KeyValuePair<string, object?>> mediumScope = new List<KeyValuePair<string, object?>>() { new("medium1", "value1"), new("medium2", "value2") };
                Dictionary<string, object?> largeScope = new() { { "large1", 1000 }, { "large2", 2000 } };

                using IDisposable? tinyLoggerScope = _logger.BeginScope("[scope is enabled]");
                _logger.LogInformation("Log with one scope");

                using IDisposable? smallLoggerScope = _logger.BeginScope(smallScope);
                _logger.LogInformation("Log with two scopes");

                using IDisposable? mediumLoggerScope = _logger.BeginScope(mediumScope);
                _logger.LogInformation("Each log message is fit in a single line.");

                using IDisposable? largeLoggerScope = _logger.BeginScope(largeScope);
                _logger.LogInformation("This is a structured {message}", "MESSAGE");

                using IDisposable? extraLargeLoggerScope = _logger.BeginScope("One more message");
                _logger.LogInformation("Add one more string scope");

                using IDisposable? ultraLargeLoggerScope = _logger.BeginScope(1234);
                _logger.LogInformation("Add an integer");
            }

            return Task.CompletedTask;
        }
    }
}
