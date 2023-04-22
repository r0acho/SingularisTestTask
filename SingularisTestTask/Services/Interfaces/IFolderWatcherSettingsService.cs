using SingularisTestTask.Settings;

namespace SingularisTestTask.Services.Interfaces;

public interface IFolderWatcherSettingsService : IDisposable
{
    FolderWatcherSettings? GetSettings { get; }
    FileSystemWatcher GetWatcher { get; }
}