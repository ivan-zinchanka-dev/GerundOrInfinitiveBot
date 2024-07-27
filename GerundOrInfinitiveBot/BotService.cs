using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GerundOrInfinitiveBot;

public class BotService
{
    private const string TelegramTokenKey = "TelegramConnectionToken";
    
    private const string StartCommand = "/start";
    private const string StopCommand = "/stop";

    private readonly IConfigurationRoot _configurationRoot; 
    private readonly ITelegramBotClient _botClient;
    private readonly ReceiverOptions _receiverOptions;
    
    public BotService(IConfigurationRoot configurationRoot)
    {
        _configurationRoot = configurationRoot;
        
        _botClient = new TelegramBotClient(_configurationRoot.GetConnectionString(TelegramTokenKey));
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
            },
           
            ThrowPendingUpdates = true,                     // enable offline 
        };
    }

    public async Task Start()
    {
        using (CancellationTokenSource cts = new CancellationTokenSource())
        {
            _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);
        
            User me = await _botClient.GetMeAsync(cancellationToken: cts.Token);
            Console.WriteLine($"{me.FirstName} is launched");
        
            await Task.Delay(-1, cts.Token);
        }
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                {
                    Message message = update.Message;
                    User sender = message.From;
                    
                    Console.WriteLine($"User: {sender.FirstName} with id {sender.Id} wrote a message: {message.Text}");

                    string answer = "____";
                    
                    using (DatabaseService database = new DatabaseService(_configurationRoot))
                    {
                        answer = database.Examples.FirstOrDefault(example => example.Id == 0).Sentence;
                    }
                    
                    await botClient.SendTextMessageAsync(message.Chat.Id, answer, cancellationToken: cancellationToken);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        if (error is ApiRequestException apiRequestException)
        {
            Console.WriteLine($"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}");
        }
        else
        {
            Console.WriteLine(error.ToString());
        }
        
        return Task.CompletedTask;
    }
    
}