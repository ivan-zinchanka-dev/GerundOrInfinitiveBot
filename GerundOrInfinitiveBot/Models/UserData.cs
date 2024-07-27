namespace GerundOrInfinitiveBot.Models;

public class UserData
{
    public long UserId { get; private set; }
    public int? CurrentExampleId { get; set; }
    public Example CurrentExample { get; set; }

    public UserData(long userId, int? currentExampleId = null)
    {
        UserId = userId;
        CurrentExampleId = currentExampleId;
    }
}