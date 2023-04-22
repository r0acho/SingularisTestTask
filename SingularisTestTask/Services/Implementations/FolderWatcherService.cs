using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using SingularisTestTask.Services.Interfaces;

namespace SingularisTestTask.Services.Implementations;

public class FolderWatcherService : IHostedService, IDisposable
{
    private readonly IFolderWatcherSettingsService _settings;
    private readonly ILogger<FolderWatcherService> _logger;
    
    private readonly List<string> _createdFiles = new();
    private readonly List<string> _updatedFiles = new();
    private readonly List<string> _deletedFiles = new();

    public FolderWatcherService(IFolderWatcherSettingsService settings, ILogger<FolderWatcherService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    
    /*private void WatcherHandler()
    {
        try
        {
            CrontabSchedule schedule = CrontabSchedule.Parse(_settings.GetSettings!.CronExpression);
            DateTime nextRunTime = schedule.GetNextOccurrence(DateTime.Now);
            TimeSpan delay = nextRunTime - DateTime.Now;
        }
        catch (CrontabException)
        {
            _logger.LogError(ErrorMessage.CantParseCronExpressionError);
        }
    }*/
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запуск FolderWatcherService...");
        SignWatcherEvents(_settings.GetWatcher);
        _logger.LogInformation("FolderWatcherService запущен");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Остановка FolderWatcherService...");
        Dispose();
        LogFinalReport();
        _logger.LogInformation("FolderWatcherService остановлен.");
        return Task.CompletedTask;
    }

    private void SignWatcherEvents(FileSystemWatcher watcher)
    {
        watcher.Created += OnChange;
        watcher.Changed += OnChange;
        watcher.Deleted += OnChange;
    }
    
    private void OnChange(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Добавлен файл {e.FullPath}");
            _createdFiles.Add(e.FullPath);
        }
        else if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Изменен файл {e.FullPath}");
            _updatedFiles.Add(e.FullPath);
        }
        else if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Удален файл {e.FullPath}");
            _deletedFiles.Add(e.FullPath);
        }
    }

    private void LogFinalReport()
    {
        if (_createdFiles.Count > 0)
        {
            _logger.LogInformation($"Добавленные файлы: {string.Join(Environment.NewLine, _createdFiles)}");
        }

        if (_updatedFiles.Count > 0)
        {
            _logger.LogInformation($"Измененные файлы: {string.Join(Environment.NewLine, _updatedFiles)}");
        }

        if (_deletedFiles.Count > 0)
        {
            _logger.LogInformation($"Удаленные файлы: {string.Join(Environment.NewLine, _deletedFiles)}");
        }
    }

    public void Dispose()
    {
        _settings.Dispose();
    }
}