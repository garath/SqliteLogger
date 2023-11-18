using Microsoft.Data.Sqlite;

namespace SqliteLogger.Tests;

internal class LoggerConnectionTests
{
    // These fields are initialized anew for each test in the Setup method, thus they will not be null.
    string _dbFilePath = null!;
    ConnectionManager _connectionManager = null!;
    SqliteLoggerConnection _loggerConnectionUnderTest = null!;
    SqliteConnection _sqliteConnectionUnderTest = null!;

    [SetUp]
    public void Setup()
    {
        _dbFilePath = Path.GetTempFileName();
        _connectionManager = new(new SqliteLoggerConfiguration()
        {
            FilePath = _dbFilePath
        });

        _loggerConnectionUnderTest = _connectionManager.CreateConnection();
        
        _sqliteConnectionUnderTest = new($"Data Source={_dbFilePath}");
        _sqliteConnectionUnderTest.Open();
    }

    [TearDown]
    public void TearDown()
    {
        _sqliteConnectionUnderTest?.Dispose();
        _loggerConnectionUnderTest?.Dispose();

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
    public void LogsTraces()
    {
        Trace trace = new()
        {
            Timestamp = new(2021, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Name = "name",
            Level = "level",
            State = "state",
            ExceptionId = null,
            Message = "message"
        };

        _loggerConnectionUnderTest.Log(trace.Timestamp, trace.Name, trace.Level, trace.State, trace.ExceptionId?.ToString(), trace.Message);

        SqliteLoggerDatabaseReader reader = new(_sqliteConnectionUnderTest);
        IEnumerable<Trace> traces = reader.ReadTraces();

        Assert.Multiple(() =>
        {
            Assert.That(traces, Has.Exactly(1).Items);
            Assert.That(traces, Has.One.EqualTo(trace));
        });
    }
}
 