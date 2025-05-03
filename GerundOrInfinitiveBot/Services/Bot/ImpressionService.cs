using System.Diagnostics;
using GerundOrInfinitiveBot.Models;
using GerundOrInfinitiveBot.Models.DataBaseObjects;

namespace GerundOrInfinitiveBot.Services.Bot;

public class ImpressionService
{
    public Example GetExampleForUser(long userId, IQueryable<Example> allExamples, IQueryable<Answer> allAnswers)
    {
        Debug.Assert(allAnswers != null);
        Debug.Assert(allExamples != null);
        
        IQueryable<int> recordedExampleIds = allAnswers
            .Select(answer => answer.ExampleId);

        IQueryable<Example> zeroImpressionExamples = allExamples
            .Where(example => !recordedExampleIds.Contains(example.Id));
        
        if (zeroImpressionExamples.Any())
        {
            return GetRandomExample(zeroImpressionExamples.ToList());
        }
        else
        {
            int selectedExampleId = GetLowestImpressionExampleIdForUser(userId, allAnswers);
            return allExamples.FirstOrDefault(example => example.Id == selectedExampleId);
        }
    }

    private int GetLowestImpressionExampleIdForUser(long userId, IQueryable<Answer> allAnswers)
    {
        IQueryable<ExampleImpressionRecord> records = allAnswers
            .Where(answer => answer.UserId == userId)
            .GroupBy(answer => answer.ExampleId)
            .Select(group => new ExampleImpressionRecord(group.Key, group.Count()));

        int minExpressionsCount = records
            .MinBy(record => record.ImpressionCount)
            .ImpressionCount;

        List<ExampleImpressionRecord> lowestImpressionRecords = records
            .Where(record => record.ImpressionCount == minExpressionsCount)
            .ToList();

        return GetRandomExampleImpressionRecord(lowestImpressionRecords).ExampleId;
    }
    
    private Example GetRandomExample(List<Example> examples)
    {
        if (examples.Count == 1)
        {
            return examples[0];
        }

        var random = new Random();
        int selectedExampleId = random.Next(0, examples.Count);
        return examples[selectedExampleId];
    }
    
    private ExampleImpressionRecord GetRandomExampleImpressionRecord(List<ExampleImpressionRecord> records)
    {
        if (records.Count == 1)
        {
            return records[0];
        }

        var random = new Random();
        int selectedExampleId = random.Next(0, records.Count);
        return records[selectedExampleId];
    }
}