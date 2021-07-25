namespace SqliteLogger
{
    public class SqliteLoggerConfiguration
    {
        public string FilePath { get; set; }
        public bool UseQueue { get; set; } = false;
    }
}
