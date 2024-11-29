using GerundOrInfinitiveBot.Services.Bot;
using GerundOrInfinitiveBot.Services.Database;
using GerundOrInfinitiveBot.Services.FileLogging;
using GerundOrInfinitiveBot.Services.Reporting;
using GerundOrInfinitiveBot.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot {
    
    internal class Program
    {
        private const string AppSettingsFileName = "appsettings.json";
        private static BotService _botService;
        
        private static async Task Main(string[] args)
        {
            IHostApplicationBuilder appBuilder = Host.CreateApplicationBuilder();

            appBuilder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(AppSettingsFileName);
            
            appBuilder.Services
                .AddLogging(builder =>
                {
                    builder
                        .AddConsole()
                        .AddFile(Path.Combine(Environment.CurrentDirectory, 
                            appBuilder.Configuration.GetValue<string>("LogsFileName")));
                })
                .AddOptions()
                .Configure<ConnectionSettings>(appBuilder.Configuration.GetSection("ConnectionStrings"))
                .Configure<EmailSettings>(appBuilder.Configuration.GetSection("EmailSettings"))
                .Configure<BotSettings>(appBuilder.Configuration.GetSection("BotSettings"))
                .AddDbContextFactory<DatabaseService>(lifetime: ServiceLifetime.Singleton)
                .AddTransient<ReportService>()
                .AddSingleton<BotService>();
            
            IServiceProvider serviceProvider = appBuilder.Services.BuildServiceProvider();
            
            _botService = serviceProvider.GetRequiredService<BotService>();
            await _botService.StartAsync();
        }
    }
}