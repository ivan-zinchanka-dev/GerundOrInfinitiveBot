namespace GerundOrInfinitiveBot.Tests;

[TestFixture]
public class ExamplesParserTest
{
    [Test]
    public async Task Execute()
    {
        var examplesParser = new ExamplesParser(@"Source\source.exm", @"SQLScripts\data.sql");
        await examplesParser.ParseAsync();
        Assert.Pass();
    }
}