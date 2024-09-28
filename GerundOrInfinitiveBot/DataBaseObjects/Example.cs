namespace GerundOrInfinitiveBot.DataBaseObjects;

public class Example
{
    private const string Gap = "...";
    public int Id { get; private set; }
    public string SourceSentence { get; set; }
    public string UsedWord { get; set; }
    public string CorrectAnswer { get; set; }
    public string AlternativeCorrectAnswer { get; set; }
    
    public ICollection<UserData> CurrentUsers { get; private set; }
    public ICollection<Answer> AnswersWithIt { get; set; }
    
    public string GetCorrectSentence() => SourceSentence.Replace(Gap, ToBold(CorrectAnswer));

    private static string ToBold(string text)
    {
        return $"<b>{text}</b>";
    }

}