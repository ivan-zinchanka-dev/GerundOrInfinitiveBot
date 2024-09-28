using GerundOrInfinitiveBot.DataBaseObjects;

namespace GerundOrInfinitiveBot.Services.Bot;

public class SessionService
{
    private const int ExamplesPerSession = 5;
    private const string SessionResultsMessagePattern = "Your session result: {0}/{1}.";
    
    private readonly IEnumerable<Answer> _answers;
    
    public SessionService(IEnumerable<Answer> answers)
    {
        _answers = answers;
    }

    public bool TryGetUserSessionResults(long userId, out string sessionResultsMessage)
    {
        if (IsUserSessionCompleted(userId))
        {
            sessionResultsMessage = GetUserSessionResultsMessage(userId);
            return true;
        }
        else
        {
            sessionResultsMessage = null;
            return false;
        }
    }

    private bool IsUserSessionCompleted(long userId)
    {
        int userAnswersCount = _answers.Count(answer => answer.UserId == userId);

        if (userAnswersCount == 0)
        {
            return false;
        }

        return userAnswersCount % ExamplesPerSession == 0;
    }

    private string GetUserSessionResultsMessage(long userId)
    {
        IEnumerable<Answer> sessionAnswers = _answers
            .Where(answer => answer.UserId == userId)
            .OrderByDescending(answer => answer.ReceivingTime)
            .Take(ExamplesPerSession);
        
        int correctAnswersCount = sessionAnswers.Count(answer => answer.Result);
        int allAnswersCount = sessionAnswers.Count();

        return string.Format(SessionResultsMessagePattern, correctAnswersCount, allAnswersCount);
    }

}