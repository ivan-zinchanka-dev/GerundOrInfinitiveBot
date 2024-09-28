using GerundOrInfinitiveBot.Models;
using GerundOrInfinitiveBot.Models.DataBaseObjects;

namespace GerundOrInfinitiveBot.Services.Bot;

public class ImpressionService
{
    private readonly IEnumerable<Answer> _answers;
    
    public ImpressionService(IEnumerable<Answer> answers)
    {
        _answers = answers;
    }
    
    public Example GetExampleForUser(long userId, List<Example> allExamples)
    {
        IEnumerable<int> recordedExampleIds = _answers
            .Select(answer => answer.ExampleId);
        
        List<Example> zeroImpressionExamples = allExamples
            .Where(example => !recordedExampleIds.Contains(example.Id))
            .ToList();
        
        if (zeroImpressionExamples.Count > 0)
        {
            Random random = new Random();
            int selectedExampleId = random.Next(0, zeroImpressionExamples.Count);
            return zeroImpressionExamples[selectedExampleId];
        }
        else
        {
            int selectedExampleId = GetLowestImpressionExampleIdForUser(userId);
            return allExamples.Find(example => example.Id == selectedExampleId);
        }
    }

    private int GetLowestImpressionExampleIdForUser(long userId)
    {
        var records = _answers
            .Where(answer => answer.UserId == userId)
            .GroupBy(answer => answer.ExampleId)
            .Select(group => new ExampleImpressionRecord(group.Key, group.Count()));

        int minExpressionsCount = records
            .MinBy(record => record.ImpressionCount)
            .ImpressionCount;

        List<ExampleImpressionRecord> lowestImpressionRecords = records
            .Where(record => record.ImpressionCount == minExpressionsCount)
            .ToList();

        if (lowestImpressionRecords.Count == 1)
        {
            return lowestImpressionRecords[0].ExampleId;
        }
        else
        {
            Random random = new Random();
            return lowestImpressionRecords[random.Next(0, lowestImpressionRecords.Count)].ExampleId;
        }
    }

}