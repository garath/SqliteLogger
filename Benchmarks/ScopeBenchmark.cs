using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using SqliteLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class ScopeBenchmark
    {
        private ILogger<Program> logger;
        private ILoggerFactory loggerFactory;

        public static IEnumerable<IReadOnlyCollection<KeyValuePair<string, object?>>> ScopeGenerator()
        {
            yield return new List<KeyValuePair<string, object?>>() { KeyValuePair.Create<string, object?>("scopeName", "scopeValue") };
            yield return new Dictionary<string, object?>() { { "key1", "value1" }, { "key2", 1234 } };
            yield return new Dictionary<string, object?>() { { "key1", "value1" }, { "key2", 1234 }, { "key3", new[] { new { a = 1, b = new { b1 = "b2" } } } } };
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSqliteLogger(options =>
                    {
                        options.FilePath = "test.db";
                    }));

            logger = loggerFactory.CreateLogger<Program>();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            loggerFactory?.Dispose();
        }

        [Benchmark]
        [ArgumentsSource(nameof(ScopeGenerator))]
        public void SqliteLoggerInMemory(IReadOnlyCollection<KeyValuePair<string, object?>> scopeCollection)
        {
            using IDisposable scope = logger.BeginScope(scopeCollection);
            logger.LogInformation("Hello World!");
        }
    }
}
