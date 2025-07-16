using GerundOrInfinitiveBot.Models.DataBaseObjects;
using GerundOrInfinitiveBot.Services.DataLoading;

namespace GerundOrInfinitiveBot.Tests.Services.DataUploading;

[TestFixture]
public class ExamplesReaderTest
{
    [Test]
    public async Task PrintExamples()
    {
        IEnumerable<Example> examples = await new ExamplesReader().UploadExamplesAsync();

        foreach (Example example in examples)
        {
            Console.WriteLine($"'{example.SourceSentence}', '{example.UsedWord}', '{example.CorrectAnswer}', '{example.AlternativeCorrectAnswer}'");
        }
    }
}