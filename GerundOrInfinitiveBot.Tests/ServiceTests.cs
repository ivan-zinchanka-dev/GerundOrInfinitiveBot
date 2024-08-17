using Microsoft.Extensions.Configuration;

namespace GerundOrInfinitiveBot.Tests;

[TestFixture]
public class ServiceTests
{
    private const string AppSettingsFileName = "appsettings.test.json";
    private IConfigurationRoot _configurationRoot;
    
    [SetUp]
    public void Configure()
    {
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.SetBasePath(Directory.GetCurrentDirectory());
        configBuilder.AddJsonFile(AppSettingsFileName);
        _configurationRoot = configBuilder.Build();
    }

    [Test]
    public async Task ReportTestException()
    {
        var testException = new Exception();
        
        try
        {
            double a = 4;
            double b = 0;
            double c = a / b;
        }
        catch (Exception ex)
        {
            testException = ex;
        }

        var reportService = new ReportService(_configurationRoot);
        bool sendingResult = await reportService.ReportException(testException);
        Assert.IsTrue(sendingResult);
    }
    
}