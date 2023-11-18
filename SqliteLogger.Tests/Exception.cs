namespace SqliteLogger.Tests;

internal class Exception
{
    public required DateTimeOffset Timestamp { get; init; }
    public required int Sequence { get; init; }
    public required Guid Id { get; init; }
    public string? Data { get; init; }
    public int? HResult { get; init; }
    public Guid? InnerExceptionId { get; init; }
    public required string Message { get; init; }
    public string? Source { get; init; }
    public string? StackTrace { get; init; }
    public string? TargetSite { get; init; }
}
