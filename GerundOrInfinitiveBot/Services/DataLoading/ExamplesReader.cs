using System.Text.RegularExpressions;
using GerundOrInfinitiveBot.Models.DataBaseObjects;

namespace GerundOrInfinitiveBot.Services.DataLoading;

public class ExamplesReader
{
    private const string SourceDataFileName = "source.exm";
    
    public async Task<IEnumerable<Example>> UploadExamplesAsync()
    {
        var examples = new List<Example>();
        string sourceDataFilePath = Path.Combine(Directory.GetCurrentDirectory(), SourceDataFileName);
        
        using (var reader = new StreamReader(sourceDataFilePath))
        {
            string currentLine;
            while ((currentLine = await reader.ReadLineAsync()) != null)
            {
                if (TryReadExample(currentLine, out Example example))
                {
                    examples.Add(example);
                }
            }
        }

        return examples;
    }
    
    private bool TryReadExample(string line, out Example example)
    {
        if (string.IsNullOrEmpty(line) || 
            string.IsNullOrWhiteSpace(line) || 
            line.StartsWith("/*") ||
            line.EndsWith("*/"))
        {
            example = null;
            return false;
        }

        MatchCollection matches = Regex.Matches(line, "'((?:[^']|'')*)'");
        var propertyValues = new List<string>();
        
        foreach (Match match in matches)
        {
            string propertyValue = match.Groups[1].Value;
            propertyValue = propertyValue.Replace("''", "'");

            propertyValues.Add(propertyValue);
        }

        if (propertyValues.Any())
        {
            example = new Example()
            {
                SourceSentence = GetValueByIndex(propertyValues, 0),
                UsedWord = GetValueByIndex(propertyValues, 1),
                CorrectAnswer = GetValueByIndex(propertyValues, 2),
                AlternativeCorrectAnswer = GetValueByIndex(propertyValues, 3),
            };

            return true;
        }
        else {
            
            example = null;
            return false;
        }
    }

    private string GetValueByIndex(List<string> propertyValues, int index)
    {
        return propertyValues.Count > index ? propertyValues[index] : null;
    }

}