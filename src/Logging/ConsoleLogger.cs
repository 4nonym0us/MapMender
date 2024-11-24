namespace MapMender.Logging;

/// <summary>
/// Basic console logger.
/// </summary>
internal class ConsoleLogger : ILogger
{
    private readonly Dictionary<LogLevel, (ConsoleColor Color, string Prefix)> _logConfig = new()
    {
        { LogLevel.Error, (ConsoleColor.Red, "ERROR") },
        { LogLevel.Warning, (ConsoleColor.Yellow, "WARN") },
        { LogLevel.Information, (ConsoleColor.Blue, "INFO") }
    };

    public void LogError(string message) => WriteLog(message, LogLevel.Error);

    public void LogWarning(string message) => WriteLog(message, LogLevel.Warning);

    public void LogInformation(string message) => WriteLog(message, LogLevel.Information);

    private void WriteLog(string message, LogLevel level)
    {
        var (color, prefix) = _logConfig[level];
        Console.ForegroundColor = color;
        Console.Write($"[{prefix}] ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    private enum LogLevel
    {
        Error,
        Warning,
        Information
    }
}