using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using SqliteLogger;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class LoggerBenchmark
    {
        private ILogger<Program> logger;
        private ILoggerFactory loggerFactory;

        [GlobalSetup(Target = nameof(SqliteLoggerInMemory))]
        public void GlobalSetupSqliteLoggerInMemory()
        {
            loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSqliteLogger(options =>
                    {
                        options.ConnectionString = "Data Source=:memory:";
                    }));

            logger = loggerFactory.CreateLogger<Program>();
        }

        [GlobalSetup(Target = nameof(SqliteLoggerFile))]
        public void GlobalSetupSqliteLoggerFile()
        {
            loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSqliteLogger(options =>
                    {
                        options.ConnectionString = "Data Source=test.db";
                    }));

            logger = loggerFactory.CreateLogger<Program>();
        }

        [GlobalSetup(Target = nameof(ConsoleLogger))]
        public void GlobalSetupConsoleDerp()
        {
            loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddConsole());

            logger = loggerFactory.CreateLogger<Program>();
        }

        [Benchmark]
        public void SqliteLoggerInMemory() => logger.LogInformation("Hello World!");

        [Benchmark]
        public void SqliteLoggerFile() => logger.LogInformation("Hello World!");

        [Benchmark (Baseline = true)]
        public void ConsoleLogger() => logger.LogInformation("Hello World!");

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            loggerFactory?.Dispose();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<LoggerBenchmark>();
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
        }
    }
}
