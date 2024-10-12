using System.Text;

namespace GerundOrInfinitiveBot.Tests.Services.ExamplesParsing;

public class ExamplesParser
{
    private const string DataScriptHead = 
        "USE gerund_or_infinitive\nGO\n\nDELETE FROM [Examples]\nGO\n\n" + 
        "INSERT INTO [Examples] ([SourceSentence], [UsedWord], [CorrectAnswer], [AlternativeCorrectAnswer])\n" +
        "VALUES\n";
    
    private const string DataRowPattern = "({0}),\n";
    private const string DataScriptEnd = "\nGO";
    
    private readonly string _inputFileName; 
    private readonly string _outputFileName;
    private readonly DirectoryInfo _solutionDirectoryInfo;
    
    public ExamplesParser(string inputFileName, string outputFileName)
    {
        _inputFileName = inputFileName;
        _outputFileName = outputFileName;
        _solutionDirectoryInfo = GetSolutionDirectoryInfo();
    }

    public async Task ParseAsync()
    {
        string inputFilePath = Path.Combine(_solutionDirectoryInfo.FullName, _inputFileName);
        string outputFilePath = Path.Combine(_solutionDirectoryInfo.FullName, _outputFileName);

        if (File.Exists(inputFilePath))
        {
            var dataScript = new StringBuilder(DataScriptHead);
            
            using (var inputFileReader = new StreamReader(inputFilePath))
            {
                string line;
                
                while ((line = inputFileReader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line == string.Empty || 
                        line.StartsWith("/*") && line.EndsWith("*/"))
                    {
                        continue;
                    }

                    dataScript.Append(string.Format(DataRowPattern, line));
                }
                
            }
            
            dataScript.Remove(dataScript.Length - 2, 1);                                // remove last ',' symbol
            dataScript.Append(DataScriptEnd);

            await File.WriteAllTextAsync(outputFilePath, dataScript.ToString());
        }
        else
        {
            throw new FileNotFoundException("Input file not found: ", inputFilePath);
        }
    }

    private static DirectoryInfo GetSolutionDirectoryInfo()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }
        return directory;
    }
}