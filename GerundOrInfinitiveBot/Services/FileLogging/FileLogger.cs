using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot.Services.FileLogging;

public class FileLogger : ILogger, IDisposable
{
    private readonly string _fullFileName;
    private readonly string _categoryName;
    private readonly object _threadLock;
    
    public FileLogger(string fullFileName, string categoryName = null)
    {
        _fullFileName = fullFileName;
        _categoryName = categoryName;
        _threadLock = new object();
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, 
        Func<TState, Exception, string> formatter)
    {
        lock (_threadLock)
        {
            if (!File.Exists(_fullFileName))
            {
                File.Create(_fullFileName);
            }

            File.AppendAllTextAsync(_fullFileName, _categoryName != null ? 
                $"[{DateTime.Now}] <{_categoryName}> {GetLogPrefix(logLevel)}: {formatter(state, exception)}\n" : 
                $"[{DateTime.Now}] {GetLogPrefix(logLevel)}: {formatter(state, exception)}\n");
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return this;
    }

    public void Dispose() { }
    
    private static string GetLogPrefix(LogLevel logType)
    {
        switch (logType)
        {
            case LogLevel.Error:
            case LogLevel.Critical:
                return "Error";
                
            case LogLevel.Warning:
                return "Warning";
                
            case LogLevel.None:
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
            default:
                return "Information";
        }
    }
}