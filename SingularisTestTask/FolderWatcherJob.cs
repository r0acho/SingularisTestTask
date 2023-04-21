using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace SingularisTestTask;

public class FolderWatcherJob : IDisposable
{
    private readonly FolderWatcherConfiguration _folderWatcherConfiguration;
    private readonly ILogger<FolderWatcherJob> _logger;
    private FileSystemWatcher? _watcher;
    
    private List<string> _createdFiles = new();
    private List<string> _updatedFiles = new();
    private List<string> _deletedFiles = new();

    
    public FolderWatcherJob(IOptions<FolderWatcherConfiguration> configuration, ILogger<FolderWatcherJob> logger)
    {
        _folderWatcherConfiguration = configuration.Value;
        _logger = logger;
    }
    
    public Task Execute()
    {
        SetupWatcher();
        return Task.CompletedTask;
    }

    private void SetupWatcher()
    {
        _watcher = new FileSystemWatcher
        {
            Path = _folderWatcherConfiguration.Path,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };

        _watcher.Created += OnChange;
        _watcher.Changed += OnChange;
        _watcher.Deleted += OnChange;

        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;
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
            _logger.LogInformation($"{Environment.NewLine}Добавленные файлы: ");
            foreach (var item in _createdFiles)
            {
                _logger.LogInformation(item);
            }
        }

        if (_updatedFiles.Count > 0)
        {
            _logger.LogInformation($"{Environment.NewLine}Измененные файлы: ");
            foreach (var item in _updatedFiles)
            {
                _logger.LogInformation(item);
            }
        }

        if (_deletedFiles.Count > 0)
        {
            _logger.LogInformation($"{Environment.NewLine}Удаленные файлы: ");
            foreach (var item in _deletedFiles)
            {
                _logger.LogInformation(item);
            }
        }
    }
    
    public void Dispose()
    {
        LogFinalReport();
        _watcher?.Dispose();
    }
}