using Microsoft.Data.Sqlite;
using System;

namespace SqliteLogger
{
    internal sealed class SqliteLoggerConnection : ILoggerConnection
    {
        private readonly SqliteConnection _connection;

        public SqliteLoggerConnection(SqliteConnection connection)
        {
            _connection = connection;
            _connection.Open();
        }

        public void Log(DateTimeOffset timestamp, string name, string level, string state, string? exceptionId, string message)
        {
            using SqliteCommand command = _connection.CreateCommand();
            command.CommandText =
                "INSERT INTO main.traces (" +
                        "timestamp, name, level, state, exception_id, message" +
                    ") VALUES (" +
                        "@timestamp, @name, @level, @state, @exceptionId, @message" +
                    ");";

            command.Parameters.AddWithValue("@timestamp", timestamp.ToString("O"));
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@level", level);
            command.Parameters.AddWithValue("@state", state);
            command.Parameters.AddWithValue("@exceptionId", exceptionId ?? DBNull.Value.ToString());
            command.Parameters.AddWithValue("@message", message);

            command.ExecuteNonQuery();
        }

        public void LogException(string timestamp, int sequence, string id, string? data, int? hresult, string? innerexceptionid, string message, string? source, string? stacktrace, string? targetsite)
        {
            using SqliteCommand command = _connection.CreateCommand();
            command.CommandText =
                "INSERT INTO main.exceptions (" +
                    "timestamp, sequence, id, data, hresult, inner_exception_id, message, source , stacktrace, targetsite" +
                ") VALUES (" +
                    "@timestamp, @sequence, @id, @data, @hresult, @innerExceptionId, @message, @source , @stacktrace, @targetsite" +
                ");";

            command.Parameters.AddWithValue("@timestamp", timestamp);
            command.Parameters.AddWithValue("@sequence", sequence);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@data", data ?? DBNull.Value.ToString());
            command.Parameters.AddWithValue("@hresult", hresult == null ? DBNull.Value : hresult);
            command.Parameters.AddWithValue("@innerExceptionId", innerexceptionid == null ? DBNull.Value : innerexceptionid);
            command.Parameters.AddWithValue("@message", message);
            command.Parameters.AddWithValue("@source", source == null ? DBNull.Value : source);
            command.Parameters.AddWithValue("@stacktrace", stacktrace == null ? DBNull.Value : stacktrace);
            command.Parameters.AddWithValue("@targetsite", targetsite == null ? DBNull.Value : targetsite);

            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection.Close();
        }
    }
}
