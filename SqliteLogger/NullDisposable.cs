using System;

namespace SqliteLogger;

internal class NullDisposable : IDisposable
{
    public static readonly IDisposable Instance = new NullDisposable();

    private NullDisposable()
    {

    }

    public void Dispose()
    {
        return;
    }
}
