using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VerusLLC.Tests;

internal static class TestHost
{
    public static ServiceProvider Create() =>
        new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning))
            .AddApplication()
            .AddPersistence()
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });
}
