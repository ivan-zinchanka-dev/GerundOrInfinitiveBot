using GerundOrInfinitiveBot.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GerundOrInfinitiveBot {
    
    internal class Program
    {
        private const string AppSettingsFileName = "appsettings.json";
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
                builder.AddConsole();
            });

            services.AddOptions();
            services.Configure<BotConnectionSettings>(config.GetSection("ConnectionStrings"));
            services.AddSingleton<BotService>();
            
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            
            _botService = serviceProvider.GetRequiredService<BotService>();
            await _botService.Start();
        }
    }
}