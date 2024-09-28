namespace GerundOrInfinitiveBot.Models;

public struct ExampleImpressionRecord
{
    public int ExampleId { get; set; }
    public int ImpressionCount { get; set; }

    public ExampleImpressionRecord(int exampleId, int impressionCount)
    {
        ExampleId = exampleId;
        ImpressionCount = impressionCount;
    }
}