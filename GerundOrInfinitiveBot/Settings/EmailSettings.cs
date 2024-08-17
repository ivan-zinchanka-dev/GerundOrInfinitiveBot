namespace GerundOrInfinitiveBot.Settings;

public class EmailSettings
{
    public string SmtpAddress { get; set; }
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string BotAddress { get; set; }
    public string BotAppPassword { get; set; }
    public string AdminAddress { get; set; }
}