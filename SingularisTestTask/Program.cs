using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SingularisTestTask.Services.Implementations;
using SingularisTestTask.Services.Interfaces;


var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); //флаг optional выставлен потому что на Main try-catch лучше не ставить
    })
    .ConfigureServices((services) =>
    {
        services.AddSingleton<IFolderWatcherSettingsService, FolderWatcherSettingsService>();
        services.AddHostedService<FolderWatcherService>();
        services.AddLogging();
    });

await builder.RunConsoleAsync();


