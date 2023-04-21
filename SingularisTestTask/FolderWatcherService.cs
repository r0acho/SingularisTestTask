using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Cronos;

namespace SingularisTestTask;

public class FolderWatcherService : IHostedService, IDisposable
{
    private readonly FolderWatcherConfiguration? _folderWatcherConfiguration;
    private readonly ILogger<FolderWatcherService> _logger;
    private FileSystemWatcher? _watcher;
    private CronExpression? _cronExpression;
    private Timer _timer;

    private readonly List<string> _createdFiles = new();
    private readonly List<string> _updatedFiles = new();
    private readonly List<string> _deletedFiles = new();

    public FolderWatcherService(IConfiguration configuration, ILogger<FolderWatcherService> logger)
    {
        _folderWatcherConfiguration = configuration.GetSection("FolderWatcher").Get<FolderWatcherConfiguration>();
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            CheckConfiguration();
            _cronExpression = CronExpression.Parse(_folderWatcherConfiguration!.CronExpression);
            _timer = new Timer(SetupWatcher, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
        catch (ConfigurationErrorsException)
        {
            _logger.LogError(ErrorMessage.JsonFileDoesntExistError);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogError(ErrorMessage.FolderDoesntExistError);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError($"{ErrorMessage.KeyNotFoundError} {ex.Message}");
        }
        catch (CronFormatException)
        {
            _logger.LogError(ErrorMessage.CantParseCronExpressionError);
        }
        catch (Exception)
        {
            _logger.LogCritical(ErrorMessage.UndefinedError);
        }
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.Dispose();

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

        return Task.CompletedTask;
    }
    
    private void SetupWatcher(object? state)
    {
        if (!Directory.Exists(_folderWatcherConfiguration!.Path))
        {
            throw new DirectoryNotFoundException();
        }

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

    private void CheckConfiguration()
    {
        if (_folderWatcherConfiguration == null)
        {
            throw new ConfigurationErrorsException();
        }

        if (_folderWatcherConfiguration.Path == null)
        {
            throw new KeyNotFoundException(ErrorMessage.PathNotFoundError);
        }

        if (_folderWatcherConfiguration.CronExpression == null)
        {
            throw new KeyNotFoundException(ErrorMessage.CronExpressionNotFoundError);
        }
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

    public void Dispose()
    {
        _watcher?.Dispose();
        _timer.Dispose();
    }
}