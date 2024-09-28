namespace GerundOrInfinitiveBot.DataBaseObjects;

public class UserData
{
    public long UserId { get; private set; }
    public int? CurrentExampleId { get; private set; }
    //public string ExampleImpressionsJson { get; set; }
    
    public Example CurrentExample { get; set; }
    public ICollection<Answer> Answers { get; set; }

    public UserData(long userId)
    {
        UserId = userId;
        //ExampleImpressionsJson = string.Empty;
    }
}