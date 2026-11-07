using VOTACAO.Infrastructure.Configuration;

namespace VOTACAO.Infrastructure.Logging;

public interface IAppLogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex = null);
}

public class FileLogger : IAppLogger
{
    private readonly string _logDir;
    private readonly object _lock = new();

    public FileLogger(AppSettings config)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _logDir = Path.Combine(baseDir, config.Logging.LogDirectory);
        if (!Directory.Exists(_logDir))
        {
            Directory.CreateDirectory(_logDir);
        }
    }

    public void LogInfo(string message) => WriteLog("INFO", message);
    public void LogWarning(string message) => WriteLog("WARN", message);
    public void LogError(string message, Exception? ex = null)
    {
        string fullMessage = ex != null ? $"{message} | Exception: {ex.Message}\nStack: {ex.StackTrace}" : message;
        WriteLog("ERROR", fullMessage);
    }

    private void WriteLog(string level, string message)
    {
        try
        {
            string fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
            string filePath = Path.Combine(_logDir, fileName);
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";

            lock (_lock)
            {
                File.AppendAllText(filePath, line);
            }
        }
        catch
        {
        }
    }
}
