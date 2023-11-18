namespace SqliteLogger.Tests;

internal class NullScopeProviderTests
{
    [Test]
    public void PushDoesNotThrow()
    {
        Assert.That(() => NullScopeProvider.Instance.Push("test"), Throws.Nothing);
    }

    [Test]
    public void ForEachDoesNotThrow()
    {
        Assert.That(() => NullScopeProvider.Instance.ForEachScope((_, _) => { }, "test"), Throws.Nothing);
    }
}
