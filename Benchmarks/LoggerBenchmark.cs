using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using SqliteLogger;
using System;
using System.Collections.Generic;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class LoggerBenchmark
    {
        private ILogger<Program> logger;
        private ILoggerFactory loggerFactory;

        [GlobalSetup(Targets = new[] { nameof(SqliteLoggerFile), nameof(SqliteLoggerInMemory) })]
        [Arguments("Data Source=test.db", "Data Source=:memory:")]
        public void GlobalSetupSqliteLoggerFile(string connectionString)
        {
            loggerFactory?.Dispose();
            loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSqliteLogger(options =>
                {
                    options.ConnectionString = connectionString;
                }));

            logger = loggerFactory.CreateLogger<Program>();
        }

        //[GlobalSetup(Target = nameof(SqliteLoggerInMemory))]
        //public void GlobalSetupSqliteLoggerInMemory()
        //{
        //    loggerFactory =
        //        LoggerFactory.Create(builder =>
        //            builder.AddSqliteLogger(options =>
        //            {
        //                options.ConnectionString = "Data Source=:memory:";
        //            }));

        //    logger = loggerFactory.CreateLogger<Program>();
        //}

        //[GlobalSetup(Target = nameof(SqliteLoggerFile))]
        //public void GlobalSetupSqliteLoggerFile()
        //{
        //    loggerFactory =
        //        LoggerFactory.Create(builder =>
        //            builder.AddSqliteLogger(options =>
        //            {
        //                options.ConnectionString = "Data Source=test.db";
        //            }));

        //    logger = loggerFactory.CreateLogger<Program>();
        //}

        [GlobalSetup(Target = nameof(ConsoleLogger))]
        public void GlobalSetupConsoleLogger()
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
}
