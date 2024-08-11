using Microsoft.Extensions.Configuration;

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
            
            _botService = new BotService(config);
            await _botService.Start();
        }
    }
}