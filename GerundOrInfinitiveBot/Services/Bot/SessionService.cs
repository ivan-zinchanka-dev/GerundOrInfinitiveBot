using System.Diagnostics;
using GerundOrInfinitiveBot.Models.DataBaseObjects;
using Microsoft.EntityFrameworkCore;

namespace GerundOrInfinitiveBot.Services.Bot;

public class SessionService
{
    public async Task<bool> IsUserSessionCompletedAsync(
        long userId, 
        IQueryable<Answer> answers, 
        int examplesPerSession = 10)
    {
        int userAnswersCount = await answers.CountAsync(answer => answer.UserId == userId);

        if (userAnswersCount == 0)
        {
            return false;
        }

        return userAnswersCount % examplesPerSession == 0;
    }

    public async Task<string> GetUserSessionResultsMessageAsync(
        long userId, 
        IQueryable<Answer> answers, 
        string sessionResultsMessagePattern, 
        int examplesPerSession = 10)
    {
        Debug.Assert(answers != null);
        
        if (await IsUserSessionCompletedAsync(userId, answers))
        {
            return await GetUserSessionResultsMessage(userId, answers, sessionResultsMessagePattern, examplesPerSession);
        }
        else
        {
            return null;
        }
    }
    
    private async Task<string> GetUserSessionResultsMessage(
        long userId, 
        IQueryable<Answer> answers, 
        string sessionResultsMessagePattern,
        int examplesPerSession = 10)
    {
        Debug.Assert(!string.IsNullOrEmpty(sessionResultsMessagePattern));
        
        IQueryable<Answer> sessionAnswers = answers
            .Where(answer => answer.UserId == userId)
            .OrderByDescending(answer => answer.ReceivingTime)
            .Take(examplesPerSession);
        
        int correctAnswersCount = await sessionAnswers.CountAsync(answer => answer.Result);
        int allAnswersCount = await sessionAnswers.CountAsync();

        return string.Format(sessionResultsMessagePattern, correctAnswersCount, allAnswersCount);
    }
}