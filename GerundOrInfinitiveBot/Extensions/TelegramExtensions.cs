using Telegram.Bot.Types;

namespace GerundOrInfinitiveBot.Extensions;

public static class TelegramExtensions
{
    public static Message ToMessage(this CallbackQuery callbackQuery)
    {
        return new Message()
        {
            From = callbackQuery.From,
            Text = callbackQuery.Data,
            Chat = callbackQuery.GetChat()
        };
    }

    private static Chat GetChat(this CallbackQuery callbackQuery)
    {
        Message message = callbackQuery.Message;
        
        if (message != null)
        {
            return message.Chat;
        }
        
        return null;
    }
}