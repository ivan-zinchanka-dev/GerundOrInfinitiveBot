namespace GerundOrInfinitiveBot.Settings;

public class BotSettings
{
    public string TaskTextPattern { get; set; }
    public string DefaultResponse { get; set; }
    public string CorrectAnswerPattern { get; set; }
    public string IncorrectAnswerPattern { get; set; }
    public string HelpMessage { get; set; }
    public string SessionResultsPattern { get; set; }
    public string NewSessionHint { get; set; }
    public string SessionStartedHint { get; set; }
    public string NewSessionButtonText { get; set; }
}