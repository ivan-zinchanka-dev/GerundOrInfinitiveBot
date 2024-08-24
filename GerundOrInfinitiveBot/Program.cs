using GerundOrInfinitiveBot.Services.Bot;
using GerundOrInfinitiveBot.Services.Database;
using GerundOrInfinitiveBot.Services.FileLogging;
using GerundOrInfinitiveBot.Services.Reporting;
using GerundOrInfinitiveBot.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot {
    
    internal class Program
    {
        private const string AppSettingsFileName = "appsettings.json";
        private const string AppLogsFileName = "applogs.log";
        
        private static BotService _botService;
        
        private static async Task Main(string[] args)
        {
            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configBuilder.AddJsonFile(AppSettingsFileName);
            IConfigurationRoot config = configBuilder.Build();

            IServiceCollection services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder
                    .AddConsole()
                    .AddFile(Path.Combine(Environment.CurrentDirectory, AppLogsFileName));
            });
            
            services.AddOptions();
            services.Configure<ConnectionSettings>(config.GetSection("ConnectionStrings"));
            services.Configure<EmailSettings>(config.GetSection("EmailSettings"));
            services.AddDbContextFactory<DatabaseService>(lifetime: ServiceLifetime.Singleton);
            services.AddTransient<ReportService>();
            services.AddSingleton<BotService>();
            
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            
            _botService = serviceProvider.GetRequiredService<BotService>();
            await _botService.Start();
        }
    }
}