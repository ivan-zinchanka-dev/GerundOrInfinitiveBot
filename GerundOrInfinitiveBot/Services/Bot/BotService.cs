using GerundOrInfinitiveBot.Extensions;
using GerundOrInfinitiveBot.Models;
using GerundOrInfinitiveBot.Models.DataBaseObjects;
using GerundOrInfinitiveBot.Services.Database;
using GerundOrInfinitiveBot.Services.Reporting;
using GerundOrInfinitiveBot.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GerundOrInfinitiveBot.Services.Bot;

public class BotService
{
    private const string StartSessionCommand = "/start";
    private const string NewExampleCommand = "/new";
    private const string HelpCommand = "/help";

    private readonly IOptions<ConnectionSettings> _connectionOptions;
    private readonly IOptions<BotSettings> _botOptions;
    private readonly ReportService _reportService;
    private readonly ImpressionService _impressionService;
    private readonly IDbContextFactory<DatabaseService> _databaseServiceFactory;
    private readonly ILogger<BotService> _logger;
    
    private readonly ITelegramBotClient _botClient;
    private readonly ReceiverOptions _receiverOptions;
    
    public BotService(
        IOptions<ConnectionSettings> connectionOptions, 
        IOptions<BotSettings> botOptions, 
        ReportService reportService, 
        ImpressionService impressionService,
        IDbContextFactory<DatabaseService> databaseServiceFactory, 
        ILogger<BotService> logger)
    {
        _connectionOptions = connectionOptions;
        _botOptions = botOptions;
        _reportService = reportService;
        _impressionService = impressionService;
        _databaseServiceFactory = databaseServiceFactory;
        _logger = logger;

        _botClient = new TelegramBotClient(_connectionOptions.Value.TelegramConnectionToken);
        _receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery,
            },
           
            ThrowPendingUpdates = true,
        };
    }

    public async Task StartAsync()
    {
        using (var cts = new CancellationTokenSource())
        {
            _botClient.StartReceiving(HandleUpdateAsync, HandleError, _receiverOptions, cts.Token);
        
            User me = await _botClient.GetMeAsync(cancellationToken: cts.Token);
            _logger.LogInformation($"{me.FirstName} is launched");
            
            await Task.Delay(-1, cts.Token);
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            Message message = update.Type switch
            {
                UpdateType.Message => update.Message,
                UpdateType.CallbackQuery => update.CallbackQuery.ToMessage(),
                _ => null
            };

            if (message != null)
            {
                await HandleMessageAsync(botClient, message, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(botClient, update, exception, cancellationToken);
        }
    }

    private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        User sender = message.From;
        
        _logger.LogInformation(
            $"User {sender.FirstName} with id {sender.Id} sent a message: {message.Text}");
        
        var responses = new Queue<BotResponse>();
        
        using (DatabaseService database = await _databaseServiceFactory.CreateDbContextAsync(cancellationToken))
        {
            if (!await database.Examples.AnyAsync(cancellationToken: cancellationToken))
            {
                throw new InvalidOperationException("Examples database is empty!");
            }

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
                case StartSessionCommand:
                    responses.Enqueue(senderData.CurrentExample == null
                        ? await GetNewExampleResponseAsync(senderData, database.Examples, database.Answers)
                        : new BotResponse(_botOptions.Value.SessionStartedHint));
                    break;
                
                case NewExampleCommand:
                    responses.Enqueue(await GetNewExampleResponseAsync(senderData, database.Examples, database.Answers));
                    break;
            
                case HelpCommand:
                    responses.Enqueue(new BotResponse(_botOptions.Value.HelpMessage));
                    break;
            
                default:

                    Example senderCurrentExample = senderData.CurrentExample;
                    
                    if (senderCurrentExample == null)
                    {
                        responses.Enqueue(new BotResponse(_botOptions.Value.DefaultResponse));
                    }
                    else if (IsAnswerCorrect(message.Text, senderCurrentExample))
                    {
                        responses.Enqueue(new BotResponse(
                            _botOptions.Value.CorrectAnswerPattern + senderCurrentExample.GetCorrectSentence()));
                        
                        database.Answers.Add(CreateAnswerEntry(sender.Id, senderCurrentExample.Id, true));
                        await database.SaveChangesAsync(cancellationToken);
                        
                        responses.Enqueue(await GetSessionsResponseAsync(database, senderData));
                    }
                    else
                    {
                        responses.Enqueue(new BotResponse(
                            _botOptions.Value.IncorrectAnswerPattern + senderCurrentExample.GetCorrectSentence()));
                        
                        database.Answers.Add(CreateAnswerEntry(sender.Id, senderCurrentExample.Id, false));
                        await database.SaveChangesAsync(cancellationToken);
                        
                        responses.Enqueue(await GetSessionsResponseAsync(database, senderData));
                    }
                    
                    break;
            }
            
            await database.SaveChangesAsync(cancellationToken);
        }
        
        while (responses.Count != 0)
        {
            BotResponse response = responses.Dequeue();
            
            await botClient.SendTextMessageAsync(message.Chat.Id, response.Text, parseMode: ParseMode.Html, 
                replyMarkup: response.ReplyMarkup, cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                $"User {sender.FirstName} with id {sender.Id} was sent a response: {response.Text}");
        }
    }
    
    private static bool IsAnswerCorrect(string answerText, Example example)
    {
        if (string.IsNullOrEmpty(answerText) || string.IsNullOrWhiteSpace(answerText))
        {
            return false;
        }

        string answer = answerText.Trim();

        return string.Equals(answer, example.CorrectAnswer, StringComparison.OrdinalIgnoreCase) || 
               string.Equals(answer, example.AlternativeCorrectAnswer, StringComparison.OrdinalIgnoreCase);
    }
    
    private static Answer CreateAnswerEntry(long userId, int exampleId, bool isCorrect)
    {
        return new Answer(Guid.NewGuid(), userId, exampleId, DateTime.UtcNow, isCorrect);
    }

    private async Task<BotResponse> GetNewExampleResponseAsync(UserData userData, DbSet<Example> examples, DbSet<Answer> answers)
    {
        userData.CurrentExample = await GetNewExampleAsync(userData, examples, answers);
        return new BotResponse(GetNewExampleMessage(userData.CurrentExample));
    }
    
    private Task<Example> GetNewExampleAsync(UserData userData, DbSet<Example> examples, DbSet<Answer> answers)
    {
        return _impressionService.GetExampleForUserAsync(userData.UserId, examples, answers);
    }

    private string GetNewExampleMessage(Example example)
    {
        return string.Format(_botOptions.Value.TaskTextPattern, example.UsedWord, example.SourceSentence);
    }
    
    private async Task<BotResponse> GetSessionsResponseAsync(DatabaseService database, UserData senderData)
    {
        SessionService sessionService = new SessionService(database.Answers, _botOptions.Value.SessionResultsPattern);

        if (sessionService.TryGetUserSessionResults(senderData.UserId, out string sessionResultsMessage))
        {
            senderData.CurrentExample = null;
            return new BotResponse(sessionResultsMessage + _botOptions.Value.NewSessionHint, CreateStartSessionButton());
        }
        else
        {
            return await GetNewExampleResponseAsync(senderData, database.Examples, database.Answers);
        }
    }

    private InlineKeyboardMarkup CreateStartSessionButton()
    {
        return new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData(_botOptions.Value.NewSessionButtonText, StartSessionCommand));
    }

    private Task HandleError(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
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
    
    private async Task HandleExceptionAsync(ITelegramBotClient botClient, Update update, Exception exception, 
        CancellationToken cancellationToken)
    {
        _logger.LogError($"Internal error!\n{exception}");
        
        await _reportService.ReportExceptionAsync(exception);
            
        if (update.Type == UpdateType.Message)
        {
            await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Internal error!\n<code>{exception}</code>", 
                parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
    }
}