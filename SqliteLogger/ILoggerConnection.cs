using System;

namespace SqliteLogger;

internal interface ILoggerConnection : IDisposable
{
    public IDisposable BeginScope();

    public void Log(DateTimeOffset timestamp, string name, string level, string state, string? exceptionId, string message);

    public void LogException(
        string timestamp, int sequence, string id, string? data, int? hResult, string? innerExceptionId, string message,
        string? source, string? stackTrace, string? targetSite);
}
