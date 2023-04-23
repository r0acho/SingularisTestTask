using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using SingularisTestTask.Services.Interfaces;

namespace SingularisTestTask.Services.Implementations;

public class FolderWatcherService : IHostedService, IDisposable
{
    private readonly IFolderWatcherSettingsService _settings;
    private readonly ILogger<FolderWatcherService> _logger;
    private Timer? _timer;

    //использовал HashSet для фильтрации дубликатов
    private readonly HashSet<string> _createdFiles = new();
    private readonly HashSet<string> _updatedFiles = new();
    private readonly HashSet<string> _deletedFiles = new();
    
    public FolderWatcherService(IFolderWatcherSettingsService settings, ILogger<FolderWatcherService> logger)
    {
        _settings = settings;
        _logger = logger;
    }
    
    /// <summary>
    /// Callback для таймера, осуществляющего ежесекундные проверки cron-расписания
    /// </summary>
    /// <param name="state"></param>
    private void CronSchedulerOnTimerCheck(object? state)
    {
        if (_settings.GetSchedule == null) return;
        var nextRunTime = _settings.GetSchedule!.GetNextOccurrence(DateTime.Now);
        var delay = nextRunTime - DateTime.Now;
        if (delay > TimeSpan.FromMinutes(1) && _settings.GetWatcher.EnableRaisingEvents == true)//время ожидания больше одной минуты - спим до включения
        {
            _logger.LogInformation($"Приложение спит до следующего включения мониторинга согласно cron-расписанию");
            _settings.GetWatcher.EnableRaisingEvents = false;
        }
        else if (delay <= TimeSpan.FromMinutes(1) && _settings.GetWatcher.EnableRaisingEvents == false) //время сна прошло, пора мониторить
        {
            //Thread.Sleep(delay); //досыпаем до начала работы
            _logger.LogInformation($"Запуск FolderWatcherService согласно расписанию");
            _settings.GetWatcher.EnableRaisingEvents = true;
        }
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Запуск FolderWatcherService...");
            SubscribeWatcherEvents(_settings.GetWatcher);
            _settings.GetWatcher.EnableRaisingEvents = true;
            _timer = new Timer(CronSchedulerOnTimerCheck, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            _logger.LogInformation("FolderWatcherService запущен");
        }
        catch (Exception)
        {
            _logger.LogError(ErrorMessage.DefaultMessage);
        }

        return Task.CompletedTask;
    }

    private TimeSpan GetSecondsToNextMinute()
    {
        return TimeSpan.FromSeconds(60 - DateTime.Now.Second);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Остановка FolderWatcherService...");
        Dispose();
        LogFinalReport();
        _logger.LogInformation("FolderWatcherService остановлен.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Подписка на события создания, изменения и удаления в отслеживаемой папке
    /// </summary>
    /// <param name="watcher"></param>
    private void SubscribeWatcherEvents(FileSystemWatcher watcher)
    {
        watcher.Created += OnChange;
        watcher.Changed += OnChange;
        watcher.Deleted += OnChange;
        watcher.Renamed += OnChange;
    }
    
    private void OnChange(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Добавлен файл {e.Name}");
            _createdFiles.Add(e.Name!);
        }
        else if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Изменен файл {e.Name}");
            _updatedFiles.Add(e.Name!);
        }
        else if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Удален файл {e.Name}");
            _deletedFiles.Add(e.Name!);
        }
        else if (e.ChangeType == WatcherChangeTypes.Renamed)
        {
            var renamedArgs = (RenamedEventArgs)e;
            _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Удален файл {renamedArgs.OldName}");
            _deletedFiles.Add(renamedArgs.OldName!);
            _logger.LogInformation($"[{DateTime.Now:HH:mm:ss}] Добавлен файл {e.Name}");
            _createdFiles.Add(e.Name!);
        }
    }

    /// <summary>
    /// Логирование итогового списка созданных, измененных и редактированных файлов
    /// </summary>
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
        _timer?.Dispose();
        _settings.Dispose();
    }
}