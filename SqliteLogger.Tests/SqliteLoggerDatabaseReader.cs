using Microsoft.Data.Sqlite;

namespace SqliteLogger.Tests;

internal class SqliteLoggerDatabaseReader
{
    private readonly SqliteConnection _connection;

    public SqliteLoggerDatabaseReader(SqliteConnection connection)
    {
        _connection = connection;
    }

    public IEnumerable<Trace> ReadTraces()
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT timestamp, name, level, state, exception_id, message FROM main.traces;";

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            yield return new Trace() {
                Timestamp = reader.GetDateTimeOffset(0),
                Name = reader.GetString(1),
                Level = reader.GetString(2),
                State = reader.GetString(3),
                ExceptionId = reader.IsDBNull(4) ? null : reader.GetGuid(4),
                Message = reader.GetString(5)
            };
        }
    }

    public IEnumerable<Exception> ReadExceptionsTable()
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT timestamp, sequence, id, data, hresult, inner_exception_id, message, source, stacktrace, targetsite FROM main.exceptions;";

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            yield return new Exception() {
                Timestamp = reader.GetDateTimeOffset(0),
                Sequence = reader.GetInt32(1),
                Id = reader.GetGuid(2),
                Data = reader.IsDBNull(3) ? null : reader.GetString(3),
                HResult = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                InnerExceptionId = reader.IsDBNull(5) ? null : reader.GetGuid(5),
                Message = reader.GetString(6),
                Source = reader.IsDBNull(7) ? null : reader.GetString(7),
                StackTrace = reader.IsDBNull(8) ? null : reader.GetString(8),
                TargetSite = reader.IsDBNull(9) ? null : reader.GetString(9)
            };
        }
    }
}