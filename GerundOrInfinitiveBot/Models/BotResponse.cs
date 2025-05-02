using Telegram.Bot.Types.ReplyMarkups;

namespace GerundOrInfinitiveBot.Models;

public readonly struct BotResponse
{
    public string Text { get; }
    public IReplyMarkup ReplyMarkup { get; }

    public BotResponse(string text, IReplyMarkup replyMarkup = null)
    {
        Text = text;
        ReplyMarkup = replyMarkup;
    }
}