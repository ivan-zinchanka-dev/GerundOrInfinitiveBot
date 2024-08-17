using GerundOrInfinitiveBot.DataBaseObjects;
using GerundOrInfinitiveBot.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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

    private readonly IOptions<BotConnectionSettings> _options; 
    private readonly ITelegramBotClient _botClient;
    private readonly ReceiverOptions _receiverOptions;
    
    public BotService(IOptions<BotConnectionSettings> options)
    {
        _options = options;
        
        _botClient = new TelegramBotClient(_options.Value.TelegramConnectionToken);
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
            },
           
            ThrowPendingUpdates = true,
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

                    Queue<string> answerTexts = new Queue<string>();
                    UserData senderData;
                    
                    using (DatabaseService database = new DatabaseService(_options.Value.SqlServerConnection))
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
                                answerTexts.Enqueue(SetNewExampleToUser(database.Examples, senderData));
                                break;
                        
                            case HelpCommand:
                                answerTexts.Enqueue(HelpMessage);
                                break;
                        
                            default:

                                Example senderCurrentExample = senderData.CurrentExample;
                                
                                if (senderCurrentExample == null)
                                {
                                    answerTexts.Enqueue(DefaultAnswer);
                                }
                                else if (IsAnswerCorrect(message, senderCurrentExample))
                                {
                                    answerTexts.Enqueue("That is correct! \ud83d\ude42\n" + 
                                                        "Corrected sentence:" + senderCurrentExample.GetCorrectSentence());
                                    answerTexts.Enqueue(SetNewExampleToUser(database.Examples, senderData));
                                }
                                else
                                {
                                    answerTexts.Enqueue("That is incorrect! \ud83d\ude41\n" + 
                                                        "Corrected sentence:" + senderCurrentExample.GetCorrectSentence());
                                    answerTexts.Enqueue(SetNewExampleToUser(database.Examples, senderData));
                                }
                                
                                break;
                        }
                        
                        await database.SaveChangesAsync(cancellationToken);
                    }

                    while (answerTexts.Count != 0)
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, answerTexts.Dequeue(), 
                            cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                    }
                    
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());

            if (update.Type == UpdateType.Message)
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                    $"Internal error!\n<code>{ex}</code>", 
                    cancellationToken: cancellationToken, parseMode: ParseMode.Html);
            }
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

    private static bool IsAnswerCorrect(Message answerMessage, Example example)
    {
        if (answerMessage == null || answerMessage.Text == null)
        {
            return false;
        }

        string answer = answerMessage.Text.Trim();
        return answer == example.CorrectAnswer || answer == example.AlternativeCorrectAnswer;
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