using System.Diagnostics.Tracing;

namespace SqliteLogger
{
    [EventSource(Name = "SqliteLogger.Queue")]
    internal sealed class QueueEventSource : EventSource
    {
        public static readonly QueueEventSource Source = new ();

        private EventCounter _drainDurationCounter;
        private EventCounter _drainRecordsCount;

        private QueueEventSource()
        {
            _drainDurationCounter = new EventCounter("drain-duration", this)
            {
                DisplayName = "Drain duration",
                DisplayUnits = "ms"
            };

            _drainRecordsCount = new EventCounter("drain-records-count", this)
            {
                DisplayName = "Records drained count"
            };
        }

        public void QueueDrainEvent(int records, long elapsedMilliseconds)
        {
            _drainDurationCounter?.WriteMetric(elapsedMilliseconds);
            _drainRecordsCount?.WriteMetric(records);
        }

        protected override void Dispose(bool disposing)
        {
            _drainDurationCounter?.Dispose();
            _drainDurationCounter = null;
            _drainRecordsCount?.Dispose();
            _drainRecordsCount = null;

            base.Dispose(disposing);
        }
    }
}
