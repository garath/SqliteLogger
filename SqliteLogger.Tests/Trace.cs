namespace SqliteLogger.Tests;

internal class Trace : IEquatable<Trace>
{
    public required DateTimeOffset Timestamp { get; set; }
    public required string Name { get; set; }
    public required string Level { get; set; }
    public required string State { get; set; }
    public Guid? ExceptionId { get; set; }
    public required string Message { get; set; }

    public bool Equals(Trace? other)
    {
        if (other is null)
        {
            return false;
        }
        
        return Timestamp.Equals(other.Timestamp)
            && Name.Equals(other.Name)
            && Level.Equals(other.Level)
            && State.Equals(other.State)
            && (ExceptionId?.Equals(other.ExceptionId) ?? (other.ExceptionId is null))
            && Message.Equals(other.Message);
    }

    public override bool Equals(object? obj) => Equals(obj as Trace);

    public override int GetHashCode() => HashCode.Combine(Timestamp, Name, Level, State, ExceptionId, Message);
}
