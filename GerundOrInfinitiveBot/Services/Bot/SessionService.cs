using System.Diagnostics;
using GerundOrInfinitiveBot.DataBaseObjects;

namespace GerundOrInfinitiveBot.Services.Bot;

public class SessionService
{
    private const int ExamplesPerSession = 5;

    private readonly Func<IEnumerable<Answer>> _answersGetter;
    
    public SessionService(Func<IEnumerable<Answer>> answersGetter)
    {
        _answersGetter = answersGetter;
    }
    
    public bool IsUserSessionCompleted(long userId)
    {
        Debug.Assert(_answersGetter != null);

        IEnumerable<Answer> answers = _answersGetter();
        
        Debug.Assert(answers != null);

        int userAnswersCount = answers.Count(answer => answer.UserId == userId);

        if (userAnswersCount == 0)
        {
            return false;
        }

        return userAnswersCount % ExamplesPerSession == 0;
    }

    public string GetUserSessionResultsMessage(long userId)
    {
        IEnumerable<Answer> answers = _answersGetter();
        
        IEnumerable<Answer> sessionAnswers = answers
            .Where(answer => answer.UserId == userId)
            .OrderByDescending(answer => answer.ReceivingTime)
            .Take(ExamplesPerSession);

        int correctAnswersCount = sessionAnswers.Count(answer => answer.Result);
        int allAnswersCount = sessionAnswers.Count();
        
        return $"Your session result: {correctAnswersCount}/{allAnswersCount}.";
    }

}