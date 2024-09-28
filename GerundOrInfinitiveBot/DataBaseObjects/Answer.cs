namespace GerundOrInfinitiveBot.DataBaseObjects;

public class Answer
{
    public Guid Id { get; private set; }
    public long UserId { get; private set; }
    public int ExampleId { get; private set; }
    public DateTime ReceivingTime { get; private set; }
    public bool Result { get; private set; }
    
    public UserData UserData { get; private set; }
    public Example Example { get; private set; }

    public Answer(Guid id, long userId, int exampleId, DateTime receivingTime, bool result)
    {
        Id = id;
        UserId = userId;
        ExampleId = exampleId;
        ReceivingTime = receivingTime;
        Result = result;
    }
}