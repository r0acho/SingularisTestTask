using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SingularisTestTask;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory());
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<FolderWatcherConfiguration>(hostContext.Configuration.GetSection("FolderWatcher"));
        services.AddScoped<FolderWatcherJob>();
        services.AddHostedService<FolderWatcherService>();
        services.AddLogging();
    });
await builder.RunConsoleAsync();