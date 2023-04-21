using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SingularisTestTask;

public class FolderWatcherService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FolderWatcherService> _logger;
    private FileSystemWatcher? _watcher;
    
    private readonly List<string> _createdFiles = new();
    private readonly List<string> _updatedFiles = new();
    private readonly List<string> _deletedFiles = new();

    private const string FolderDoesntExistError = "Указанная папка не существует";
    private const string JsonFileDoesntExistError = "Конфигурационный файл appsettings.json не найден";
    private const string PathNotFoundError = "Путь к папке не указан в конфигурационном файле";

    public FolderWatcherService(IConfiguration configuration, ILogger<FolderWatcherService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            string folderPath = _configuration["FolderWatcher:Path"] ?? throw new KeyNotFoundException();
            SetupWatcher(folderPath);
        }
        catch (FileNotFoundException)
        {
            _logger.LogError(JsonFileDoesntExistError);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogError(FolderDoesntExistError);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogError(PathNotFoundError);
        }
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Добавленные файлы: {Environment.NewLine}");
        foreach (var item in _createdFiles)
        {
            _logger.LogInformation(item);
        }
        
        _logger.LogInformation($"Измененные файлы: {Environment.NewLine}");
        foreach (var item in _updatedFiles)
        {
            _logger.LogInformation(item);
        }
        
        _logger.LogInformation($"Удаленные файлы: {Environment.NewLine}");
        foreach (var item in _deletedFiles)
        {
            _logger.LogInformation(item);
        }
        
        _watcher?.Dispose();
        return Task.CompletedTask;
    }
    
    private void SetupWatcher(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            _logger.LogError(FolderDoesntExistError);
            throw new DirectoryNotFoundException();
        }

        _watcher = new FileSystemWatcher
        {
            Path = folderPath,
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
}