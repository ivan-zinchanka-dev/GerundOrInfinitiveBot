using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot.Tests.Services.Logging;

public class TestLogger<T> : ILogger<T>, IDisposable
{
    private readonly object _threadLock;
    
    public TestLogger()
    {
        _threadLock = new object();
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, 
        Func<TState, Exception, string> formatter)
    {
        lock (_threadLock)
        {
            Console.WriteLine($"{GetLogPrefix(logLevel)}: {formatter(state, exception)}\n");
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