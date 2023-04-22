using NCrontab;
using SingularisTestTask.Settings;

namespace SingularisTestTask.Services.Interfaces;

public interface IFolderWatcherSettingsService : IDisposable
{
    CrontabSchedule? GetSchedule { get; }
    FileSystemWatcher GetWatcher { get; }
}