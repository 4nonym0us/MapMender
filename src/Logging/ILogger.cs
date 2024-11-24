namespace MapMender.Logging;

/// <summary>
/// Basic logging abstraction.
/// </summary>
public interface ILogger
{
    void LogError(string message);

    void LogWarning(string message);

    void LogInformation(string message);
}