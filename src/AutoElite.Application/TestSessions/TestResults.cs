namespace AutoElite.Application.TestSessions;

/// <summary>
/// Hides the results of an attempt in progress: the correct answers, the explanations and the counts are shown only
/// after the attempt (FR-7.7), to the student and to the staff alike.
/// </summary>
internal static class TestResults
{
    /// <summary>Hides the counts of an attempt in progress.</summary>
    /// <param name="session">The attempt.</param>
    public static TestSessionItem Hide(TestSessionItem session) =>
        session.Status == TestSessionStatus.InProgress ? session with { CorrectAnswerCount = null, MistakeCount = null } : session;

    /// <summary>Hides the counts, the correct answers and the explanations of an attempt in progress.</summary>
    /// <param name="details">The card of the attempt.</param>
    public static TestSessionDetails Hide(TestSessionDetails details)
    {
        if (details.Session.Status != TestSessionStatus.InProgress)
            return details;

        return details with
        {
            Session = Hide(details.Session),
            Questions =
            [
                .. details.Questions.Select(question =>
                    question with { CorrectOptionId = null, IsCorrect = null, Explanation = null })
            ],
        };
    }

    /// <summary>Determines whether the caller may read the attempts of a student's contract.</summary>
    /// <param name="currentUser">The caller.</param>
    /// <param name="studentId">The student of the contract.</param>
    /// <param name="branchId">The branch of the contract, if any.</param>
    public static bool MayRead(ICurrentUser currentUser, Guid studentId, Guid? branchId)
    {
        if (currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager) && DataScope.For(currentUser).Includes(branchId))
            return true;

        return studentId == currentUser.UserId;
    }
}
