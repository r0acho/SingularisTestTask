using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SingularisTestTask.Services.Implementations;
using SingularisTestTask.Services.Interfaces;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("FolderSettings.json", optional: false, reloadOnChange: true);
        foreach (var provider in config.Sources)
        {
            if (provider is FileConfigurationSource fileSource)
            {
                Console.WriteLine($"Loaded configuration file: {fileSource.Path}");
            }
        }
    })
    .ConfigureServices((services) =>
    {
        services.AddSingleton<IFolderWatcherSettingsService, FolderWatcherSettingsService>();
        services.AddHostedService<FolderWatcherService>();
        services.AddLogging();
    });

await builder.RunConsoleAsync();