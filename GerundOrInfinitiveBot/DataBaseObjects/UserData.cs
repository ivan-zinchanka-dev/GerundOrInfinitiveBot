namespace GerundOrInfinitiveBot.DataBaseObjects;

public class UserData
{
    public long UserId { get; private set; }
    public int? CurrentExampleId { get; private set; }
    public Example CurrentExample { get; set; }
    public string ExampleImpressionsJson { get; set; }
    
    public UserData(long userId)
    {
        UserId = userId;
        ExampleImpressionsJson = string.Empty;
    }
}