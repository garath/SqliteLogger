using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using SqliteLogger;

namespace Benchmarks
{
    [MarkdownExporterAttribute.GitHub]
    class LoggerBenchmark
    {
        private ILogger<Program> logger;
        private ILoggerFactory loggerFactory;

        [Params("Data Source=:memory:", "Data Source=test.db")]
        public string ConnectionString { get; set; }

        [GlobalSetup(Target = nameof(Derp))]
        public void GlobalSetupDerp()
        {
            loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSqliteLogger(options =>
                    {
                        options.ConnectionString = ConnectionString;
                    }));

            logger = loggerFactory.CreateLogger<Program>();
        }

        [GlobalSetup(Target = nameof(ConsoleDerp))]
        public void GlobalSetupConsoleDerp()
        {
            loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddConsole());

            logger = loggerFactory.CreateLogger<Program>();
        }

        [Benchmark]
        public void Derp() => logger.LogInformation("Hello World!");

        //[Benchmark (Baseline = true)]
        public void ConsoleDerp() => logger.LogInformation("Hello World!");

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
