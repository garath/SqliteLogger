using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SqliteLogger.Tests;

internal class ExtensionTests
{
    [Test]
    public void LoggerAndConfigIsAddedToServiceCollection()
    {
        string dbFilePath = Path.GetTempFileName();

        ServiceProvider services = new ServiceCollection()
            .AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSqliteLogger(config => { config.FilePath = dbFilePath; });
            })
            .BuildServiceProvider();

        ILoggerProvider[] providers = services.GetServices<ILoggerProvider>().ToArray();
        ILoggerFactory? loggerFactory = services.GetService<ILoggerFactory>();

        SqliteLoggerConfiguration? actualConfiguration = services.GetService<IOptions<SqliteLoggerConfiguration>>()?.Value;

        Assert.Multiple(() =>
        {
            Assert.That(providers, Has.One.InstanceOf<SqliteLoggerProvider>());
            Assert.That(actualConfiguration, Is.Not.Null);
        });
    }
}