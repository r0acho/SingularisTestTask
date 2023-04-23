using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NCrontab;
using SingularisTestTask.Services.Interfaces;
using SingularisTestTask.Settings;

namespace SingularisTestTask.Services.Implementations;

public class FolderWatcherSettingsService : IFolderWatcherSettingsService
{
    private readonly ILogger<FolderWatcherSettingsService> _logger;
    private readonly IDisposable _changeTokenRegistration;
    private readonly FileSystemWatcher _watcher;
    
    private FolderWatcherSettings? _settings;
    private CrontabSchedule? _schedule;
    
    private const string ConfigFileEdited = "Конфигурационный файл изменен";

    public FolderWatcherSettings? GetSettings => _settings;
    public CrontabSchedule? GetSchedule => _schedule;
    public FileSystemWatcher GetWatcher => _watcher;

    public FolderWatcherSettingsService(IConfiguration configuration, ILogger<FolderWatcherSettingsService> logger)
    {
        _settings = configuration.GetSection("FolderWatcher").Get<FolderWatcherSettings>(); //считываем секцию "FolderWatcher" из конфигурации
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
            });//перезагружаем конфигурацию в случае изменения настроек в файле и проверяем их на валидность
    }

    /// <summary>
    /// Метод настройки мониторинга папки и парсинга cron-расписания
    /// </summary>
    private void SetWatcherSettings()
    {
        try
        {
            CheckConfiguration();
            _watcher.Path = _settings!.Path!;
            _schedule = CrontabSchedule.Parse(_settings.CronExpression);
        }
        catch (ConfigurationErrorsException)
        {
            _logger.LogError(ErrorMessage.SectionSettingsNotFoundError);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogError(ErrorMessage.FolderDoesntExistError);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError($"{ErrorMessage.KeyNotFoundError} {ex.Message}");
        }
        catch (CrontabException)
        {
            _logger.LogError(ErrorMessage.CantParseCronExpressionError);
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
    
    /// <summary>
    /// Метод для проверки конфигурации перед настройкой мониторинга
    /// </summary>
    /// <exception cref="ConfigurationErrorsException"></exception>
    /// <exception cref="KeyNotFoundException"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
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