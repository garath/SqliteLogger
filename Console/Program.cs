using Microsoft.Extensions.Logging;
using SqliteLogger;
using System;
using System.Collections.Generic;

namespace Console
{
    class Program
    {
        static void Main(string[] args)
        {
            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    }).AddSqliteLogger(options =>
                    {
                        options.ConnectionString = "Data Source=test.db";
                    }));


            // Logging scopes
            ScopeLoggingExamples scopeLoggingExamples = new(loggerFactory.CreateLogger<ScopeLoggingExamples>());
            scopeLoggingExamples.DoExamples();

            // Structured Logging
            StructuredLoggingExamples structuredLoggingExamples = new(loggerFactory.CreateLogger<StructuredLoggingExamples>());
            structuredLoggingExamples.DoExamples();
        }
    }

    class ScopeLoggingExamples
    {
        // Inspiration from NLog
        // https://github.com/NLog/NLog.Extensions.Logging/wiki/NLog-properties-with-Microsoft-Extension-Logging

        // Ideally, a Dictionary<string, object> for nice JSONification
        // Otherwise, construct a string.

        readonly ILogger<ScopeLoggingExamples> _logger;

        public ScopeLoggingExamples(ILogger<ScopeLoggingExamples> logger)
        {
            _logger = logger;
        }

        public void DoExamples()
        {
            KeyValuePair<string, int> smallScope = new("PropertyThing", 123);
            List<KeyValuePair<string, string>> mediumScope = new() { new("medium1", "value1"), new("medium2", "value2") };
            Dictionary<string, long> largeScope = new() { { "large1", 1000 }, { "large2", 2000 } };

            using IDisposable tinyLoggerScope = _logger.BeginScope("[scope is enabled]");
            _logger.LogInformation("Log with one scope");

            using IDisposable smallLoggerScope = _logger.BeginScope(smallScope);
            _logger.LogInformation("Log with two scopes");

            using IDisposable mediumLoggerScope = _logger.BeginScope(mediumScope);
            _logger.LogInformation("Each log message is fit in a single line.");

            using IDisposable largeLoggerScope = _logger.BeginScope(largeScope);
            _logger.LogInformation("This is a structured {message}", "MESSAGE");
        }


    }

    class StructuredLoggingExamples
    {
        readonly ILogger<StructuredLoggingExamples> _logger;

        public StructuredLoggingExamples(ILogger<StructuredLoggingExamples> logger)
        {
            _logger = logger;
        }

        public void DoExamples()
        {
            // Inspiration from NLog
            // https://github.com/nlog/nlog/wiki/How-to-use-structured-logging#formatting-of-the-message
            // It's possible to control formatting by preceding @ or $:
            //  @ will format the object as JSON
            //  $ forces ToString()

            Object o = null;

            // null case. Result:  Test NULL
            _logger.LogInformation("Test {value1}", o);

            // datetime case. Result:  Test 25-3-2018 00:00:00 (locale TString)
            _logger.LogInformation("Test {value1}", new DateTime(2018, 03, 25));

            // list of strings. Result: Test "a", "b"
            _logger.LogInformation("Test {value1}", new List<string> { "a", "b" });

            // array. Result: Test "a", "b"
            _logger.LogInformation("Test {value1}", new[] { "a", "b" });

            // dict. Result:  Test "key1"=1, "key2"=2
            _logger.LogInformation("Test {value1}", new Dictionary<string, int> { { "key1", 1 }, { "key2", 2 } });

            var order = new
            {
                OrderId = 2,
                Status = OrderStatus.Processing
            };

            // object Result:  Test MyProgram.Program+Order
            _logger.LogInformation("Test {value1}", order);

            // object Result:  Test {"OrderId":2, "Status":"Processing"}
            _logger.LogInformation("Test {@value1}", order);

            // anomynous object. Result: Test { OrderId = 2, Status = Processing }
            _logger.LogInformation("Test {value1}", new { OrderId = 2, Status = "Processing" });

            // anomynous object. Result:Test {"OrderId":2, "Status":"Processing"}
            _logger.LogInformation("Test {@value1}", new { OrderId = 2, Status = "Processing" });
        }

        enum OrderStatus
        {
            Processing
        };
    }
}
