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
                UpdateType.CallbackQuery,
            },
           
            ThrowPendingUpdates = true,
        };
    }

    public async Task Start()
    {
        using (CancellationTokenSource cts = new CancellationTokenSource())
        {
            _botClient.StartReceiving(HandleUpdate, HandleError, _receiverOptions, cts.Token);
        
            User me = await _botClient.GetMeAsync(cancellationToken: cts.Token);
            _logger.LogInformation($"{me.FirstName} is launched");
            
            await Task.Delay(-1, cts.Token);
        }
    }

    private async Task HandleUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            Message message = update.Type switch
            {
                UpdateType.Message => update.Message,
                UpdateType.CallbackQuery => QueryToMessage(update.CallbackQuery),
                _ => null
            };

            if (message != null)
            {
                await HandleMessage(botClient, message, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            await HandleException(botClient, update, exception, cancellationToken);
        }
    }

    private static Message QueryToMessage(CallbackQuery callbackQuery)
    {
        return new Message()
        {
            From = callbackQuery.From,
            Text = callbackQuery.Data,
            Chat = callbackQuery.Message.Chat
        };
    }

    private async Task HandleMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        User sender = message.From;
        
        _logger.LogInformation(
            $"User {sender.FirstName} with id {sender.Id} sent a message: {message.Text}");
        
        Queue<BotResponse> responses = new Queue<BotResponse>();
        
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
                        ? GetNewExampleResponse(senderData, database.Examples, database.Answers)
                        : new BotResponse(_botOptions.Value.SessionStartedHint));
                    break;
                
                case NewExampleCommand:
                    responses.Enqueue(GetNewExampleResponse(senderData, database.Examples, database.Answers));
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
                        
                        responses.Enqueue(GetSessionsResponse(database, senderData));
                    }
                    else
                    {
                        responses.Enqueue(new BotResponse(
                            _botOptions.Value.IncorrectAnswerPattern + senderCurrentExample.GetCorrectSentence()));
                        
                        database.Answers.Add(CreateAnswerEntry(sender.Id, senderCurrentExample.Id, false));
                        await database.SaveChangesAsync(cancellationToken);
                        
                        responses.Enqueue(GetSessionsResponse(database, senderData));
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
                $"User {sender.FirstName} with id {sender.Id} was sent a response: {response}");
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

    private BotResponse GetNewExampleResponse(UserData userData, DbSet<Example> examples, DbSet<Answer> answers)
    {
        userData.CurrentExample = GetNewExample(userData, examples, answers);
        return new BotResponse()
        {
            Text = GetNewExampleMessage(userData.CurrentExample)
        };
    }
    
    private static Example GetNewExample(UserData userData, DbSet<Example> examples, DbSet<Answer> answers)
    {
        ImpressionService impressionService = new ImpressionService(answers);
        return impressionService.GetExampleForUser(userData.UserId, examples.ToList());
    }

    private string GetNewExampleMessage(Example example)
    {
        return string.Format(_botOptions.Value.TaskTextPattern, example.UsedWord, example.SourceSentence);
    }

    private static Answer CreateAnswerEntry(long userId, int exampleId, bool isCorrect)
    {
        return new Answer(Guid.NewGuid(), userId, exampleId, DateTime.UtcNow, isCorrect);
    }
    
    private BotResponse GetSessionsResponse(DatabaseService database, UserData senderData)
    {
        SessionService sessionService = new SessionService(database.Answers, _botOptions.Value.SessionResultsPattern);

        if (sessionService.TryGetUserSessionResults(senderData.UserId, out string sessionResultsMessage))
        {
            senderData.CurrentExample = null;
            return new BotResponse()
            {
                Text = sessionResultsMessage + _botOptions.Value.NewSessionHint,
                ReplyMarkup = CreateStartSessionButton(),
            };
        }
        else
        {
            return GetNewExampleResponse(senderData, database.Examples, database.Answers);
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
    
    private async Task HandleException(ITelegramBotClient botClient, Update update, Exception exception, 
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

    private struct BotResponse
    {
        public string Text { get; set; }
        public IReplyMarkup ReplyMarkup { get; set; }

        public BotResponse(string text)
        {
            Text = text;
        }
    }

}