using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;


namespace SingularisTestTask;

public class FolderWatcherService : IHostedService
{
    private readonly FolderWatcherConfiguration? _folderWatcherConfiguration;
    private readonly ILogger<FolderWatcherService> _logger;
    private readonly FolderWatcherJob _job;
    
    public FolderWatcherService(IOptions<FolderWatcherConfiguration> configuration, ILogger<FolderWatcherService> logger, FolderWatcherJob job)
    {
        _folderWatcherConfiguration = configuration.Value;
        _logger = logger;
        _job = job;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            CheckConfiguration();
            CrontabSchedule schedule = CrontabSchedule.Parse(_folderWatcherConfiguration!.CronExpression);
            DateTime nextRunTime = schedule.GetNextOccurrence(DateTime.Now);
            TimeSpan delay = nextRunTime - DateTime.Now;

            /*await using (Timer timer = new Timer((_) =>
            {
                using (_job)
                {
                     _job.Execute();
                    Thread.Sleep(delay);
                }
            }))
            {
            }*/
            using (_job)
            {
                await _job.Execute();
                Thread.Sleep(delay);
            }
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

    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping FolderWatcherService...");
        _job.Dispose();
        _logger.LogInformation("FolderWatcherService stopped.");
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
        
        if (!Directory.Exists(_folderWatcherConfiguration!.Path))
        {
            throw new DirectoryNotFoundException();
        }
    }
    

}