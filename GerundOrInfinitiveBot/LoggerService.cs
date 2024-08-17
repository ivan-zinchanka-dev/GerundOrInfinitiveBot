using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot;

public class LoggerService : ILogger, IDisposable
{
    private readonly string _fullFileName;
    private readonly object _threadLock;
    //public static ILoggerFactory GetFactory()
    
    public LoggerService(string fullFileName)
    {
        _fullFileName = fullFileName;
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
                
            File.AppendAllTextAsync(_fullFileName,
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