using Microsoft.Data.Sqlite;
using System;

namespace SqliteLogger;

internal class SqliteCommandsScope : IDisposable
{
    private readonly SqliteTransaction _transacion;

    public SqliteCommandsScope(SqliteTransaction transaction)
    {
        _transacion = transaction;
    }

    public void Attach(SqliteCommand command)
    {
        command.Transaction = _transacion;
    }

    public void Dispose()
    {
        _transacion.Commit();
        _transacion.Dispose();
    }
}
