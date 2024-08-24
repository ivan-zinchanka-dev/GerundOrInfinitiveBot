using GerundOrInfinitiveBot.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GerundOrInfinitiveBot.Tests;

[TestFixture]
public class ServiceTests
{
    private const string AppSettingsFileName = "appsettings.test.json";
    private EmailSettings _emailSettings;
    
    [SetUp]
    public void Configure()
    {
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        configBuilder.SetBasePath(Directory.GetCurrentDirectory());
        configBuilder.AddJsonFile(AppSettingsFileName, false, true);
        IConfigurationRoot configurationRoot = configBuilder.Build();
        
        _emailSettings = configurationRoot.GetSection("EmailSettings").Get<EmailSettings>();
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

        var reportService = new ReportService(Options.Create<EmailSettings>(_emailSettings));
        bool sendingResult = await reportService.ReportExceptionAsync(testException);
        Assert.IsTrue(sendingResult);
    }
    
}