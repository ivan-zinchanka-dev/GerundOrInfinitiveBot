using Microsoft.EntityFrameworkCore;

namespace GerundOrInfinitiveBot.Models;

[PrimaryKey(nameof(Id))]
public class Example
{
    private const string Gap = "...";
    
    public int Id { get; private set; }
    public string SourceSentence { get; set; }
    public string UsedWord { get; set; }
    public string CorrectAnswer { get; set; }

    public string GetCorrectSentence => SourceSentence.Replace(Gap, CorrectAnswer);
}