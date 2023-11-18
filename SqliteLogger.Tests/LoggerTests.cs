using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SqliteLogger.Tests;

internal class LoggerTests
{
    // These fields are initialized anew for each test in the Setup method, thus they will not be null.
    string _dbFilePath = null!;
    ServiceProvider _serviceProvider = null!;

    SqliteConnection _sqliteConnectionUnderTest = null!;

    [SetUp]
    public void Setup()
    {
        //string connectionString = "Data Source=:memory:";
        _dbFilePath = Path.GetTempFileName();
        _serviceProvider = new ServiceCollection()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSqliteLogger(config => { config.FilePath = _dbFilePath; });
            })
            .BuildServiceProvider();

        _sqliteConnectionUnderTest = new($"Data Source={_dbFilePath}");
        _sqliteConnectionUnderTest.Open();
    }

    [TearDown]
    public void TearDown()
    {
        _sqliteConnectionUnderTest?.Dispose();
        _serviceProvider?.Dispose();

        if (File.Exists(_dbFilePath))
        {
            try
            {
                File.Delete(_dbFilePath);
            }
            catch (IOException)
            {
                // Do nothing
            }
        }
    }

    [Test]
    public void Test()
    {
        ILogger<LoggerTests> logger = _serviceProvider.GetRequiredService<ILogger<LoggerTests>>();

        DateTimeOffset logTime = DateTimeOffset.Now;
        logger.Log(LogLevel.Information, "message");

        SqliteLoggerDatabaseReader reader = new(_sqliteConnectionUnderTest);
        IEnumerable<Trace> traces = reader.ReadTraces();

        Assert.Multiple(() =>
        {
            Assert.That(traces, Has.Exactly(1).Items);
            
            Trace trace = traces.First();
            Assert.That(trace.Timestamp, Is.EqualTo(logTime).Within(TimeSpan.FromSeconds(1)));
            Assert.That(trace.Name, Is.EqualTo(GetType().FullName));
            Assert.That(trace.Level, Is.EqualTo("Information"));
            Assert.That(trace.ExceptionId, Is.Null);
            Assert.That(trace.Message, Is.EqualTo("message"));
            Assert.That(trace.State, Is.EqualTo("{\"{OriginalFormat}\":\"message\"}"));
        });
    }

    [Test]
    public void LogsException()
    {
        ILogger<LoggerTests> logger = _serviceProvider.GetRequiredService<ILogger<LoggerTests>>();

        System.Exception exception = new("exception");
        DateTimeOffset logTime = DateTimeOffset.Now;
        logger.Log(LogLevel.Information, exception, "message");

        SqliteLoggerDatabaseReader reader = new(_sqliteConnectionUnderTest);
        Trace[] traces = reader.ReadTraces().ToArray();
        Exception[] exceptions = reader.ReadExceptionsTable().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(traces, Has.Exactly(1).Items);

            Trace trace = traces.First();
            Assert.That(trace.ExceptionId, Is.Not.Null);

            Assert.That(exceptions, Has.Exactly(1).Items);
            Exception exception = exceptions.First();
            Assert.That(exception.Id, Is.EqualTo(trace.ExceptionId));
            Assert.That(exception.Message, Is.EqualTo("exception"));
        });
    }

    [Test]
    public void LogsChainedException()
    {
        // Create the logger under test
        ILogger<LoggerTests> logger = _serviceProvider.GetRequiredService<ILogger<LoggerTests>>();

        // Create two exceptions, one nested inside the other
        System.Exception innerException = new("innerException");
        System.Exception outerException = new("outerException", innerException);

        // Act: Log a simple trace with a paired exception
        logger.Log(LogLevel.Information, outerException, "message");

        SqliteLoggerDatabaseReader reader = new(_sqliteConnectionUnderTest);
        Trace[] traces = reader.ReadTraces().ToArray();
        Exception[] exceptions = reader.ReadExceptionsTable().ToArray();

        // Verify: The size of the tables can only have the expected data (no extra rows), assuming the rest of the tests pass.
        Assert.Multiple(() =>
        {
            Assert.That(traces, Has.Exactly(1).Items);
            Assert.That(exceptions, Has.Exactly(2).Items);
        });
        
        // Verify: The initial trace contains a reference to the outer exception
        Trace? trace = null;
        Assert.Multiple(() =>
        {
            trace = traces.First();
            Assert.That(trace.ExceptionId, Is.Not.Null.Or.Empty);
        });

        // Verify: the outer exception with the expected identity exists in the table
        Exception? outerEx = null;
        Assert.Multiple(() =>
        {
            outerEx = exceptions.FirstOrDefault(ex => ex.Id == trace!.ExceptionId);
            Assert.That(outerEx, Is.Not.Null);
        });

        // Verify: the outer exception has the expected properties and a reference to the inner exception
        Assert.Multiple(() =>
        {
            Assert.That(outerEx!.InnerExceptionId, Is.Not.Null.Or.Empty);
            Assert.That(outerEx!.Message, Is.EqualTo("outerException"));
            Assert.That(outerEx!.Sequence, Is.EqualTo(0));
        });

        // Verify: The inner exception with the expected identity exists in the table
        Exception? innerEx = null;
        Assert.Multiple(() =>
        {
            innerEx = exceptions.FirstOrDefault(ex => ex.Id == outerEx!.InnerExceptionId);
            Assert.That(innerEx, Is.Not.Null);
        });

        // Verify: the inner exception has the expected properties
        Assert.Multiple(() =>
        { 
            Assert.That(innerEx!.InnerExceptionId, Is.Null);
            Assert.That(innerEx!.Message, Is.EqualTo("innerException"));
            Assert.That(innerEx!.Sequence, Is.EqualTo(1));
        });
    }

    [Test]
    public void LogsGenericScope()
    {
        // Scopes of an arbitrary string should be appended to the values of the Scope element

        ILogger<LoggerTests> logger = _serviceProvider.GetRequiredService<ILogger<LoggerTests>>();

        using (logger.BeginScope("testScope"))
        {
            DateTimeOffset logTime = DateTimeOffset.Now;
            logger.Log(LogLevel.Information, "message");
        }

        SqliteLoggerDatabaseReader reader = new(_sqliteConnectionUnderTest);
        Trace trace = reader.ReadTraces().First();

        Assert.That(trace.State, Does.Contain("\"Scope\":[\"testScope\"]"));
    }

    [Test]
    public void LogsKeyValuePairScope()
    {
        // Scopes of a single KeyValuePair<string, object?> should be serialized as JSON objects

        ILogger<LoggerTests> logger = _serviceProvider.GetRequiredService<ILogger<LoggerTests>>();

        using (logger.BeginScope(KeyValuePair.Create<string, object?>("scopeKey", "scopeValue")))
        {
            DateTimeOffset logTime = DateTimeOffset.Now;
            logger.Log(LogLevel.Information, "message");
        }

        SqliteLoggerDatabaseReader reader = new(_sqliteConnectionUnderTest);
        Trace trace = reader.ReadTraces().First();

        Assert.That(trace.State, Does.Contain("\"scopeKey\":\"scopeValue\""));
    }

    [Test]
    public void LogsKeyValuePairScopeWithNullDoesNotThrow()
    {
        // Scopes of a single KeyValuePair<string, object?> should be serialized as JSON objects

        ILogger<LoggerTests> logger = _serviceProvider.GetRequiredService<ILogger<LoggerTests>>();

        using (logger.BeginScope(KeyValuePair.Create<string, object?>("scopeKey", null)))
        {
            Assert.That(() => logger.Log(LogLevel.Information, "message"), Throws.Nothing);
        }

        SqliteLoggerDatabaseReader reader = new(_sqliteConnectionUnderTest);
        Trace trace = reader.ReadTraces().First();

        Assert.That(trace.State, Does.Contain("\"scopeKey\":null"));
    }

    [Test]
    public void LogsDictionaryScope()
    {
        // Scopes of type Dictionary<string, object?> should be serialized as JSON objects

        ILogger<LoggerTests> logger = _serviceProvider.GetRequiredService<ILogger<LoggerTests>>();

        Dictionary<string, object?> scopes = new() { { "scopeKey1", "scopeValue1" }, { "scopeKey2", "scopeValue2" } };
        using (logger.BeginScope(scopes))
        {
            DateTimeOffset logTime = DateTimeOffset.Now;
            logger.Log(LogLevel.Information, "message");
        }

        SqliteLoggerDatabaseReader reader = new(_sqliteConnectionUnderTest);
        Trace trace = reader.ReadTraces().First();

        Assert.That(trace.State, Does.Contain("\"scopeKey1\":\"scopeValue1\"").And.Contain("\"scopeKey2\":\"scopeValue2\""));
    }
}
