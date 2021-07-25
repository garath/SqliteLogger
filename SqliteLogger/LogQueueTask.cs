using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SqliteLogger
{
    internal class LogQueueTask
    {
        private readonly SqliteConnection _source;

        public LogQueueTask(SqliteConnection source)
        {
            _source = source;
        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            Stopwatch drainDuration = new();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(500, CancellationToken.None);

                drainDuration.Restart();
                using SqliteCommand command = _source.CreateCommand();
                command.CommandText =
                    "BEGIN IMMEDIATE TRANSACTION; " +
                    "INSERT INTO file.traces SELECT * FROM main.traces; " +
                    "DELETE FROM main.traces; " +
                    "COMMIT;";
                int rowsAffected = command.ExecuteNonQuery();

                QueueEventSource.Source.QueueDrainEvent(rowsAffected, drainDuration.ElapsedMilliseconds);
            }
        }
    }
}
