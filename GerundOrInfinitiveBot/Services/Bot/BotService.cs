using System.Text.Json;
using GerundOrInfinitiveBot.DataBaseObjects;
using GerundOrInfinitiveBot.Models;
using GerundOrInfinitiveBot.Services.Database;
using GerundOrInfinitiveBot.Services.Reporting;
using GerundOrInfinitiveBot.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GerundOrInfinitiveBot.Services.Bot;

public class BotService
{
    private const string StartCommand = "/start";
    private const string HelpCommand = "/help";

    private readonly IOptions<ConnectionSettings> _connectionOptions;
    private readonly IOptions<BotSettings> _botOptions;
    private readonly ReportService _reportService;
    private readonly IDbContextFactory<DatabaseService> _databaseServiceFactory;
    private readonly ILogger<BotService> _logger;
    
    private readonly ITelegramBotClient _botClient;
    private readonly ReceiverOptions _receiverOptions;
    
    public BotService(IOptions<ConnectionSettings> connectionOptions, IOptions<BotSettings> botOptions, 
        ReportService reportService, IDbContextFactory<DatabaseService> databaseServiceFactory, 
        ILogger<BotService> logger)
    {
        _connectionOptions = connectionOptions;
        _botOptions = botOptions;
        _reportService = reportService;
        _databaseServiceFactory = databaseServiceFactory;
        _logger = logger;
        
        _botClient = new TelegramBotClient(_connectionOptions.Value.TelegramConnectionToken);
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
            _logger.LogInformation($"{me.FirstName} is launched");
            
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
                    
                    _logger.LogInformation(
                        $"User {sender.FirstName} with id {sender.Id} sent a message: {message.Text}");
                    
                    Queue<string> answerTexts = new Queue<string>();
                    
                    using (DatabaseService database = await _databaseServiceFactory.CreateDbContextAsync(cancellationToken))
                    {
                        UserData foundUserData = database.UserData
                            .Include(userData => userData.CurrentExample)
                            .FirstOrDefault(userData => userData.UserId == sender.Id);

                        UserData senderData;
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
                                answerTexts.Enqueue(_botOptions.Value.HelpMessage);
                                break;
                        
                            default:

                                Example senderCurrentExample = senderData.CurrentExample;
                                
                                if (senderCurrentExample == null)
                                {
                                    answerTexts.Enqueue(_botOptions.Value.DefaultAnswer);
                                }
                                else if (IsAnswerCorrect(message, senderCurrentExample))
                                {
                                    answerTexts.Enqueue("That is correct! \ud83d\ude42\n" + 
                                                        "Corrected sentence: " + senderCurrentExample.GetCorrectSentence());
                                    answerTexts.Enqueue(SetNewExampleToUser(database.Examples, senderData));
                                }
                                else
                                {
                                    answerTexts.Enqueue("That is incorrect! \ud83d\ude41\n" + 
                                                        "Corrected sentence: " + senderCurrentExample.GetCorrectSentence());
                                    answerTexts.Enqueue(SetNewExampleToUser(database.Examples, senderData));
                                }
                                
                                break;
                        }
                        
                        await database.SaveChangesAsync(cancellationToken);
                    }
                    
                    while (answerTexts.Count != 0)
                    {
                        string answerText = answerTexts.Dequeue();
                        
                        await botClient.SendTextMessageAsync(message.Chat.Id, answerText, 
                            cancellationToken: cancellationToken, parseMode: ParseMode.Html);
                        
                        _logger.LogInformation(
                            $"User {sender.FirstName} with id {sender.Id} was sent a response: {answerText}");
                    }
                    
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Internal error!\n{ex}");
        
            await _reportService.ReportExceptionAsync(ex);
            
            if (update.Type == UpdateType.Message)
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                    $"Internal error!\n<code>{ex}</code>", 
                    cancellationToken: cancellationToken, parseMode: ParseMode.Html);
            }
        }
    }

    private string SetNewExampleToUser(DbSet<Example> examples, UserData userData)
    {
        List<ExampleImpressionRecord> exampleImpressionRecords = new List<ExampleImpressionRecord>();
        
        if (!string.IsNullOrWhiteSpace(userData.ExampleImpressionsJson))
        {
            exampleImpressionRecords = 
                JsonSerializer.Deserialize<List<ExampleImpressionRecord>>(userData.ExampleImpressionsJson) ?? 
                new List<ExampleImpressionRecord>();
        }
        
        ImpressionService impressionService = new ImpressionService(exampleImpressionRecords);
        Example example = SelectNewExample(examples.ToList(), impressionService);
        impressionService.AppendExampleImpression(example.Id);
        
        userData.CurrentExample = example;
        userData.ExampleImpressionsJson = JsonSerializer.Serialize(impressionService.ExampleImpressionRecords);
        
        return string.Format(_botOptions.Value.TaskTextPattern, example.UsedWord, example.SourceSentence);
    }

    private static Example SelectNewExample(List<Example> examples, ImpressionService impressionService)
    {
        if (examples.IsNullOrEmpty())
        {
            return null;
        }
        
        IEnumerable<int> recordedExampleIds = impressionService.ExampleImpressionRecords.Select(record => record.ExampleId);
        List<Example> zeroImpressionExamples = examples.Where(example => !recordedExampleIds.Contains(example.Id)).ToList();

        Random random = new Random();
        
        if (zeroImpressionExamples.Count > 0)
        {
            int selectedExampleId = random.Next(0, zeroImpressionExamples.Count);
            return zeroImpressionExamples[selectedExampleId];
        }
        else
        {
            List<ExampleImpressionRecord> lowestImpressionRecords =
                impressionService.GetLowestExampleImpressionRecords();

            int selectedExampleId = lowestImpressionRecords[random.Next(0, lowestImpressionRecords.Count)].ExampleId;
            return examples.Find(example => example.Id == selectedExampleId);
        }
        
    }

    private static bool IsAnswerCorrect(Message answerMessage, Example example)
    {
        if (answerMessage == null || answerMessage.Text == null)
        {
            return false;
        }

        string answer = answerMessage.Text.Trim();

        return string.Equals(answer, example.CorrectAnswer, StringComparison.OrdinalIgnoreCase) || 
               string.Equals(answer, example.AlternativeCorrectAnswer, StringComparison.OrdinalIgnoreCase);
    }

    private Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        if (error is ApiRequestException apiRequestException)
        {
            _logger.LogError($"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}");
        }
        else
        {
            _logger.LogError($"Error:\n{error}");
        }
        
        return Task.CompletedTask;
    }
    
}