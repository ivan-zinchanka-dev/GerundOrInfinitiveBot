using System.Diagnostics;
using GerundOrInfinitiveBot.Models.DataBaseObjects;

namespace GerundOrInfinitiveBot.Services.Bot;

public class SessionService
{
    private const int ExamplesPerSession = 5;
    
    private readonly IEnumerable<Answer> _answers;
    private readonly string _sessionResultsMsgPattern;
    
    public SessionService(IEnumerable<Answer> answers, string sessionResultsMsgPattern)
    {
        _answers = answers;
        _sessionResultsMsgPattern = sessionResultsMsgPattern;
    }

    public bool TryGetUserSessionResults(long userId, out string sessionResultsMessage)
    {
        Debug.Assert(_answers != null);
        
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
        Debug.Assert(!string.IsNullOrEmpty(_sessionResultsMsgPattern));
        
        IEnumerable<Answer> sessionAnswers = _answers
            .Where(answer => answer.UserId == userId)
            .OrderByDescending(answer => answer.ReceivingTime)
            .Take(ExamplesPerSession);
        
        int correctAnswersCount = sessionAnswers.Count(answer => answer.Result);
        int allAnswersCount = sessionAnswers.Count();

        return string.Format(_sessionResultsMsgPattern, correctAnswersCount, allAnswersCount);
    }

}