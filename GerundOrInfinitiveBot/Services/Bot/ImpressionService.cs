using System.Diagnostics;
using GerundOrInfinitiveBot.Models;
using GerundOrInfinitiveBot.Models.DataBaseObjects;

namespace GerundOrInfinitiveBot.Services.Bot;

// TODO Async
public class ImpressionService
{
    public Example GetExampleForUser(long userId, IQueryable<Example> allExamples, IQueryable<Answer> allAnswers)
    {
        Debug.Assert(allAnswers != null);
        Debug.Assert(allExamples != null);
        
        IQueryable<int> recordedExampleIds = allAnswers
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

        if (lowestImpressionRecords.Count == 1)
        {
            return lowestImpressionRecords[0].ExampleId;
        }
        else
        {
            var random = new Random();
            return lowestImpressionRecords[random.Next(0, lowestImpressionRecords.Count)].ExampleId;
        }
    }

}