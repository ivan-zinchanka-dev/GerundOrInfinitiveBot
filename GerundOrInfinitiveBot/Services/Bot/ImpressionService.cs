using System.Diagnostics;
using GerundOrInfinitiveBot.Models;
using GerundOrInfinitiveBot.Models.DataBaseObjects;
using Microsoft.EntityFrameworkCore;

namespace GerundOrInfinitiveBot.Services.Bot;

public class ImpressionService
{
    public async Task<Example> GetExampleForUserAsync(long userId, IQueryable<Example> allExamples, IQueryable<Answer> allAnswers)
    {
        Debug.Assert(allAnswers != null);
        Debug.Assert(allExamples != null);
        
        IQueryable<int> recordedExampleIds = allAnswers
            .Select(answer => answer.ExampleId);

        IQueryable<Example> zeroImpressionExamples = allExamples
            .Where(example => !recordedExampleIds.Contains(example.Id));

        bool anyZeroImpressionExamples = await zeroImpressionExamples.AnyAsync();
        
        if (anyZeroImpressionExamples)
        {
            List<Example> castExamples = await zeroImpressionExamples.ToListAsync();
            return GetRandomExample(castExamples);
        }
        else
        {
            int selectedExampleId = await GetLowestImpressionExampleIdForUser(userId, allAnswers);
            return await allExamples.FirstOrDefaultAsync(example => example.Id == selectedExampleId);
        }
    }

    private async Task<int> GetLowestImpressionExampleIdForUser(long userId, IQueryable<Answer> allAnswers)
    {
        IQueryable<ExampleImpressionRecord> records = allAnswers
            .Where(answer => answer.UserId == userId)
            .GroupBy(answer => answer.ExampleId)
            .Select(group => new ExampleImpressionRecord(group.Key, group.Count()));

        int minExpressionsCount = await records
            .Select(record => record.ImpressionCount)
            .MinAsync();

        List<ExampleImpressionRecord> lowestImpressionRecords = await records
            .Where(record => record.ImpressionCount == minExpressionsCount)
            .ToListAsync();

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