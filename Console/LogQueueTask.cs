using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Console
{
    public class LogQueueTask
    {
        private readonly SqliteConnection _source;
        private readonly SqliteConnection _destination;

        public LogQueueTask(SqliteConnection source, SqliteConnection destination)
        {

        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;


        }
    }
}
