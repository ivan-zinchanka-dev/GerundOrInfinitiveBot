namespace GerundOrInfinitiveBot.Models.DataBaseObjects;

public class UserData
{
    public long UserId { get; private set; }
    public int? CurrentExampleId { get; private set; }
    
    public Example CurrentExample { get; set; }
    public ICollection<Answer> Answers { get; set; }

    public UserData(long userId)
    {
        UserId = userId;
    }
}