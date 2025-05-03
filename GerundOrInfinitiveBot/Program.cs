using GerundOrInfinitiveBot.Services.Bot;
using GerundOrInfinitiveBot.Services.Database;
using GerundOrInfinitiveBot.Services.Reporting;
using GerundOrInfinitiveBot.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace GerundOrInfinitiveBot {
    
    internal class Program
    {
        private const string AppSettingsFileName = "appsettings.json";
        private static BotService _botService;
        
        private static async Task Main(string[] args)
        {
            try
            {
                IHostApplicationBuilder appBuilder = Host.CreateApplicationBuilder();

                appBuilder.Configuration
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(AppSettingsFileName);

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(appBuilder.Configuration)
                    .CreateLogger();

                appBuilder.Logging
                    .ClearProviders()
                    .AddSerilog(Log.Logger);

                appBuilder.Services
                    .AddOptions()
                    .Configure<ConnectionSettings>(appBuilder.Configuration.GetSection("ConnectionStrings"))
                    .Configure<EmailSettings>(appBuilder.Configuration.GetSection("EmailSettings"))
                    .Configure<BotSettings>(appBuilder.Configuration.GetSection("BotSettings"))
                    .AddTransient<ReportService>()
                    .AddTransient<ImpressionService>()
                    .AddTransient<SessionService>()
                    .AddDbContextFactory<DatabaseService>(lifetime: ServiceLifetime.Singleton)
                    .AddSingleton<BotService>();

                IServiceProvider serviceProvider = appBuilder.Services.BuildServiceProvider();

                _botService = serviceProvider.GetRequiredService<BotService>();
                await _botService.StartAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled exception occured");
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}