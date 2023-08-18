using Microsoft.Data.Sqlite;
using System;
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

        public TimeSpan Delay { get; set; }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Delay, CancellationToken.None);

                SqliteCommand command = _source.CreateCommand();
                command.CommandText =
                    "BEGIN IMMEDIATE TRANSACTION; " +
                    "INSERT INTO file.exceptions SELECT * FROM main.exceptions; " +
                    "DELETE FROM main.exceptions; " +
                    "INSERT INTO file.traces SELECT * FROM main.traces; " +
                    "DELETE FROM main.traces; " +
                    "COMMIT;";
                command.ExecuteNonQuery();
            }
        }
    }
}
