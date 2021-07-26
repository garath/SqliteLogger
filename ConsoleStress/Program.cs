using Microsoft.Extensions.Hosting;
using System;
using SqliteLogger;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleStress
{
    class Program
    {
        static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(loggingConfiguration =>
                {
                    loggingConfiguration.AddSqliteLogger(config => );
                })
                .Build();
        }
    }

    internal class LoggingStressWorker : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}
