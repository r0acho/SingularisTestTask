using NCrontab;
using SingularisTestTask.Settings;

namespace SingularisTestTask.Services.Interfaces;

public interface IFolderWatcherSettingsService : IDisposable
{
    CrontabSchedule? Schedule { get; }
    FileSystemWatcher Watcher { get; }
}