using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot.Services.FileLogging;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _fullFileName;

    public FileLoggerProvider(string fullFileName)
    {
        _fullFileName = fullFileName;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_fullFileName);
    }
    
    public void Dispose() { }
}