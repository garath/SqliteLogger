using System;

namespace SqliteLogger
{
    public class SqliteLoggerConfiguration
    {
        public string FilePath { get; set; }

        public bool UseQueue { get; set; } = false;

        public TimeSpan DelayBetweenQueueDrain { get; set; } = TimeSpan.FromMilliseconds(500.0); // min value = 1 msec
    }
}
