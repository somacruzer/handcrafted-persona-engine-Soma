using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using PersonaEngine.Lib;
using PersonaEngine.Lib.Core;

namespace PersonaEngine.App;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
                      .SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", false, true);

        IConfiguration config   = builder.Build();
        var            services = new ServiceCollection();

        services.AddLogging(loggingBuilder =>
                            {
                                loggingBuilder.AddSimpleConsole(options =>
                                                                {
                                                                    options.ColorBehavior   = LoggerColorBehavior.Enabled;
                                                                    options.SingleLine      = true;
                                                                    options.TimestampFormat = "HH:mm:ss ";
                                                                });

                                loggingBuilder.SetMinimumLevel(LogLevel.Information);
                            });

        services.AddApp(config);

        var serviceProvider = services.BuildServiceProvider();

        var window = serviceProvider.GetRequiredService<AvatarApp>();
        window.Run();

        await serviceProvider.DisposeAsync();
    }
}