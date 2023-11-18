namespace SqliteLogger.Tests;

internal class NullDisposableTests
{
    [Test]
    public void DisposeDoesNotThrow()
    {
        Assert.That(NullDisposable.Instance.Dispose, Throws.Nothing);
    }
}
