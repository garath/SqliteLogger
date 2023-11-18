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
                {
                    builder.AddSqliteLogger(options =>
                    {
                        options.FilePath = "test.db";
                        options.UseQueue = true;
                    });

                    //builder.AddApplicationInsights();

                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    });
                });


            // Logging scopes
            ScopeLoggingExamples scopeLoggingExamples = new(loggerFactory.CreateLogger<ScopeLoggingExamples>());
            scopeLoggingExamples.DoExamples();

            // Structured Logging
            StructuredLoggingExamples structuredLoggingExamples = new(loggerFactory.CreateLogger<StructuredLoggingExamples>());
            structuredLoggingExamples.DoExamples();

            // Exceptions
            ExceptionsLoggingExamples exceptionsLoggingExamples = new(loggerFactory.CreateLogger<ExceptionsLoggingExamples>());
            exceptionsLoggingExamples.DoExamples();
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

            Object? o = null;

            _logger.LogDebug("null case. Result:  Test NULL");
            _logger.LogInformation("Test {value1}", o);

            _logger.LogDebug("datetime case. Result:  Test 25-3-2018 00:00:00 (locale TString)");
            _logger.LogInformation("Test {value1}", new DateTime(2018, 03, 25));

            _logger.LogDebug("list of strings. Result: Test \"a\", \"b\"");
            _logger.LogInformation("Test {value1}", new List<string> { "a", "b" });

            _logger.LogDebug("// array. Result: Test \"a\", \"b\"");
            _logger.LogInformation("Test {value1}", new[] { "a", "b" });

            _logger.LogDebug("dict. Result:  Test \"key1\"=1, \"key2\"=2");
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

    class ExceptionsLoggingExamples
    {
        readonly ILogger<ExceptionsLoggingExamples> _logger;

        public ExceptionsLoggingExamples(ILogger<ExceptionsLoggingExamples> logger)
        {
            _logger = logger;
        }

        public void DoExamples()
        {
            _logger.LogError(
                new ArgumentNullException("nullParamName", "Oh no a null exception"),
                "I think someone just shot a torpedo at us");

            _logger.LogWarning(
                new ArgumentNullException("Oh no a null exception", new InvalidOperationException("This is an invalid operation exception")),
                "I think someone just shot a torpedo at us"
            );

            try
            {
                int i = Array.Empty<int>()[1];
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex, "I caused an {exception}", "IndexOutOfRangeException");
            }
        }
    }
}
