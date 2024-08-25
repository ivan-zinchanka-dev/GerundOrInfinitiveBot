using GerundOrInfinitiveBot.Models;

namespace GerundOrInfinitiveBot.Services.Bot;

public class ImpressionService
{
    public List<ExampleImpressionRecord> ExampleImpressionRecords { get; private set; }

    public ImpressionService(List<ExampleImpressionRecord> exampleImpressionRecords)
    {
        ExampleImpressionRecords = exampleImpressionRecords;
    }

    public ExampleImpressionRecord GetImpressionByExampleId(int exampleId)
    {
        return ExampleImpressionRecords.Find(record => record.ExampleId == exampleId);
    }

    public void AppendExampleImpression(int selectedExampleId)
    {
        int recordIndex = ExampleImpressionRecords.FindIndex(record => record.ExampleId == selectedExampleId);
            
        if (recordIndex != -1)
        {
            ExampleImpressionRecord foundRecord = ExampleImpressionRecords[recordIndex];
            foundRecord.ImpressionCount++;
            ExampleImpressionRecords[recordIndex] = foundRecord;
        }
        else
        {
            ExampleImpressionRecords.Add(new ExampleImpressionRecord(selectedExampleId, 1));
        }
    }

    public List<ExampleImpressionRecord> GetLowestExampleImpressionRecords()
    {
        return ExampleImpressionRecords
            .GroupBy(record => record.ImpressionCount)
            .MinBy(group => group.Key)
            .ToList();
    }
}