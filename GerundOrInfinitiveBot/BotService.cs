﻿using GerundOrInfinitiveBot.Models;
using Microsoft.EntityFrameworkCore;
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
                            database.SaveChanges();
                        }
                        else
                        {
                            senderData = foundUserData;
                        }
                        
                        switch (message.Text)
                        {
                            case StartCommand:
                                Example example = GetNewExample();
                                answerText = string.Format(TaskTextPattern, example.UsedWord, example.SourceSentence);
                                senderData.CurrentExample = example;
                                database.SaveChanges();
                                break;
                        
                            case HelpCommand:
                                answerText = HelpMessage;
                                break;
                        
                            default:

                                if (senderData.CurrentExample == null)
                                {
                                    answerText = DefaultAnswer;
                                }
                                else if(message.Text.Trim() == senderData.CurrentExample.CorrectAnswer)
                                {
                                    answerText = "That is correct!";
                                }
                                else
                                {
                                    answerText = "That is wrong!";
                                }
                                
                                break;
                        }
                    }
                    
                    await botClient.SendTextMessageAsync(message.Chat.Id, answerText, cancellationToken: cancellationToken);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void RegisterUserIfNeed(long userId)
    {
        
        
        using (DatabaseService database = new DatabaseService(_configurationRoot))
        {
            UserData foundUserData = database.UserData.FirstOrDefault(userData => userData.UserId == userId);

            if (foundUserData == null)
            {
                database.UserData.Add(new UserData(userId));
            }

        }
        
    }


    private Example GetNewExample()
    {
        Example foundExample = null;
        
        using (DatabaseService database = new DatabaseService(_configurationRoot))
        {
            Random random = new Random();
            int examplesCount = database.Examples.Count();
            
            do
            {
                int randomId = random.Next(0, examplesCount);
                foundExample = database.Examples.FirstOrDefault(example => example.Id == randomId);

            } while (foundExample == null);
        }
        
        return foundExample;
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