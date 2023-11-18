﻿using BenchmarkDotNet.Attributes;
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
        private ILogger<Program> logger = null!;
        private ILoggerFactory loggerFactory = null!;

        [ParamsAllValues]
        public bool UseQueue;

        [GlobalSetup(Target = nameof(SqliteLogger))]
        public void GlobalSetupSqliteLoggerFile()
        {
            loggerFactory?.Dispose();
            loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSqliteLogger(options =>
                {
                    options.FilePath = "test.db";
                    options.UseQueue = UseQueue;
                }));

            logger = loggerFactory.CreateLogger<Program>();
        }

        [GlobalSetup(Target = nameof(ConsoleLogger))]
        public void GlobalSetupConsoleLogger()
        {
            loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddConsole());

            logger = loggerFactory.CreateLogger<Program>();
        }

        [Benchmark]
        public void SqliteLogger() => logger.LogInformation("Hello World!");

        [Benchmark (Baseline = true)]
        public void ConsoleLogger() => logger.LogInformation("Hello World!");

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            loggerFactory?.Dispose();
        }
    }
}
