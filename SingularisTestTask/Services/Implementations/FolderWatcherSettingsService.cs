using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SingularisTestTask.Services.Interfaces;
using SingularisTestTask.Settings;

namespace SingularisTestTask.Services.Implementations;

public class FolderWatcherSettingsService : IFolderWatcherSettingsService
{
    private readonly ILogger<FolderWatcherSettingsService> _logger;
    private readonly IDisposable _changeTokenRegistration;
    private readonly FileSystemWatcher _watcher;
    
    private FolderWatcherSettings? _settings;
    private const string ConfigFileEdited = "Конфигурационный файл изменен";

    public FolderWatcherSettings? GetSettings => _settings;
    public FileSystemWatcher GetWatcher => _watcher;

    public FolderWatcherSettingsService(IConfiguration configuration, ILogger<FolderWatcherSettingsService> logger)
    {
        _settings = configuration.GetSection("FolderWatcher").Get<FolderWatcherSettings>();
        _watcher = new FileSystemWatcher
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            IncludeSubdirectories = true,
        };
        _logger = logger;
        SetWatcherSettings();
        _changeTokenRegistration = ChangeToken.OnChange(
            configuration.GetReloadToken,
            () =>
            {
                _logger.LogInformation(ConfigFileEdited);
                _settings = configuration.GetSection("FolderWatcher").Get<FolderWatcherSettings>();
                SetWatcherSettings();
            });
    }

    private void SetWatcherSettings()
    {
        try
        {
            CheckConfiguration();
            _watcher.Path = _settings!.Path!;
            _watcher.EnableRaisingEvents = true;
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
        catch (Exception ex)
        {
            _logger.LogCritical(ErrorMessage.UndefinedError);
            _logger.LogCritical(ex.Message);
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _changeTokenRegistration.Dispose();
    }
    
    private void CheckConfiguration()
    {
        if (_settings == null)
        {
            throw new ConfigurationErrorsException();
        }

        if (_settings.Path == null)
        {
            throw new KeyNotFoundException(ErrorMessage.PathNotFoundError);
        }

        if (_settings.CronExpression == null)
        {
            throw new KeyNotFoundException(ErrorMessage.CronExpressionNotFoundError);
        }
        
        if (!Directory.Exists(_settings!.Path))
        {
            throw new DirectoryNotFoundException();
        }
    }
}