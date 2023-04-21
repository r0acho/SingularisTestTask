namespace SingularisTestTask;

public static class ErrorMessage
{
    public const string FolderDoesntExistError = "Указанная папка не существует";
    public const string JsonFileDoesntExistError = "Конфигурационный файл appsettings.json не найден";
    public const string KeyNotFoundError = "Не найдено значение в конфигурационном файле: ";
    public const string CronExpressionNotFoundError = "cron-выражение не указано";
    public const string PathNotFoundError = "путь к папке не указан";
    public const string CantParseCronExpressionError = "cron-выражение задано в неверном формате";
    public const string UndefinedError = "Неизвестная ошибка";
}