using Microsoft.Data.Sqlite;
using System;

namespace SqliteLogger;

internal static class SqliteCommandExtensions
{
    internal static void AattachToScope(this SqliteCommand command, SqliteCommandsScope? scope)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        scope?.Attach(command);
    }
}
