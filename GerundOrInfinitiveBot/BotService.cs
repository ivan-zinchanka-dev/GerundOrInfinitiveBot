using GerundOrInfinitiveBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GerundOrInfinitiveBot;

public class BotService
{
    private const string TelegramTokenKey = "TelegramConnectionToken";

    private const string TaskTextPattern = "Complete the sentence with a verb \"{0}\" in correct form.\n{1}";
    private const string DefaultAnswer = "To get help use \"/help\" command";
    private const string HelpMessage = "[HelpMessage]";
    
    private const string StartCommand = "/start";
    private const string HelpCommand = "/help";

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

                    string answerText = null;
                    UserData senderData;
                    
                    using (DatabaseService database = new DatabaseService(_configurationRoot))
                    {
                        UserData foundUserData = database.UserData
                            .Include(userData => userData.CurrentExample)
                            .FirstOrDefault(userData => userData.UserId == sender.Id);

                        if (foundUserData == null)
                        {
                            senderData = new UserData(sender.Id);
                            database.UserData.Add(senderData);
                        }
                        else
                        {
                            senderData = foundUserData;
                        }
                        
                        switch (message.Text)
                        {
                            case StartCommand:
                                answerText = SetNewExampleToUser(database.Examples, senderData);
                                break;
                        
                            case HelpCommand:
                                answerText = HelpMessage;
                                break;
                        
                            default:

                                Example senderCurrentExample = senderData.CurrentExample;
                                
                                if (senderCurrentExample == null)
                                {
                                    answerText = DefaultAnswer;
                                }
                                else if(message.Text.Trim() == senderCurrentExample.CorrectAnswer)
                                {
                                    answerText = "That is correct! \ud83d\ude42\n"
                                                 + $"Corrected sentence: {senderCurrentExample.GetCorrectSentence()}\n"
                                                 + SetNewExampleToUser(database.Examples, senderData);
                                }
                                else
                                {
                                    answerText = "That is incorrect! \ud83d\ude41\n" 
                                                 + $"Corrected sentence: {senderCurrentExample.GetCorrectSentence()}\n"
                                                 + SetNewExampleToUser(database.Examples, senderData);
                                }
                                
                                break;
                        }
                        
                        await database.SaveChangesAsync(cancellationToken);
                    }
                    
                    await botClient.SendTextMessageAsync(message.Chat.Id, answerText, 
                        cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                    
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static string SetNewExampleToUser(DbSet<Example> examples, UserData userData)
    {
        Example example = GetNewExample(examples.ToList());
        userData.CurrentExample = example;
        return string.Format(TaskTextPattern, example.UsedWord, example.SourceSentence);
    }

    private static Example GetNewExample(IReadOnlyList<Example> examples)
    {
        if (examples.IsNullOrEmpty())
        {
            return null;
        }
        
        Random random = new Random();
        return examples[random.Next(0, examples.Count)];
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