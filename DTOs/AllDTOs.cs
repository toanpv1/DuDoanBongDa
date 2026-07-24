namespace WorldCupPredictor.DTOs
{
    // ===== AUTH =====
    public record LoginRequest(string Username, string Password);
    public record LoginResponse(string Token, string DisplayName, string Role, int UserId);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    // ===== USER =====
    public record CreateUserRequest(string Username, string Password, string DisplayName, string? Email, string Role);
    public record UpdateUserRequest(string? DisplayName, string? Email, string? Role, bool? IsActive);
    public record ResetPasswordRequest(string NewPassword);
    public record UserDto(int Id, string Username, string DisplayName, string? Email, string Role, bool IsActive, DateTime CreatedAt);

    // ===== TOURNAMENT =====
    public record CreateTournamentRequest(string Name, string? Description, string PredictionType, string? PointCalculationMethod, DateTime StartDate, DateTime EndDate);
    public record UpdateTournamentRequest(string? Name, string? Description, string? PredictionType, string? PointCalculationMethod, DateTime? StartDate, DateTime? EndDate, bool? IsActive);
    public record TournamentDto(int Id, string Name, string? Description, string PredictionType, string PointCalculationMethod, DateTime StartDate, DateTime EndDate, bool IsActive, int MemberCount, int MatchCount);

    // ===== TOURNAMENT MEMBER =====
    public record AddMemberRequest(int UserId, string MissedMatchPolicy, double MissedMatchPercentage);

    // ===== ROUND =====
    public record CreateRoundRequest(string Name, string ShortName, int PointsForCorrect, int SortOrder);
    public record UpdateRoundRequest(string? Name, string? ShortName, int? PointsForCorrect, int? SortOrder);
    public record RoundDto(int Id, string Name, string ShortName, int PointsForCorrect, int SortOrder, int MatchCount);

    // ===== MATCH =====
    public record CreateMatchRequest(int TournamentId, int RoundId, string? GroupName, string HomeTeam, string AwayTeam, string? HomeFlag, string? AwayFlag, DateTime MatchDate);
    public record UpdateMatchResultRequest(int HomeScore, int AwayScore);
    public record MatchDto(int Id, int TournamentId, int RoundId, string RoundName, string? GroupName,
        string HomeTeam, string AwayTeam, string? HomeFlag, string? AwayFlag,
        DateTime MatchDate, DateTime PredictionDeadline,
        int? HomeScore, int? AwayScore, string Status, bool IsPredictionOpen,
        int PointsForCorrect, string PredictionType, MyPredictionDto? MyPrediction);

    // ===== MATCH STATS =====
    public record MatchPredictionStatDto(int UserId, string DisplayName, int? PredictedHomeScore, int? PredictedAwayScore, string? PredictedResult, bool? IsCorrect, double PointsEarned);
    public record MatchStatsDto(int MatchId, int TotalPredictions, int CorrectCount, int WrongCount, List<MatchPredictionStatDto> Predictions);

    // ===== PREDICTION =====
    public record SubmitPredictionRequest(int MatchId, int? PredictedHomeScore, int? PredictedAwayScore, string? PredictedResult);
    public record MyPredictionDto(int Id, int? PredictedHomeScore, int? PredictedAwayScore, string? PredictedResult, double PointsEarned, bool? IsCorrect);

    // ===== LEADERBOARD =====
    public record LeaderboardEntry(int Rank, int UserId, string DisplayName, double TotalPoints,
        int TotalMatches, int CorrectPredictions, int WrongPredictions, int MissedPredictions, double Accuracy);

    // ===== STATISTICS =====
    public record PersonalStats(double TotalPoints, int TotalMatches, int CorrectPredictions,
        int WrongPredictions, int MissedPredictions, double Accuracy,
        List<RoundStats> RoundBreakdown, List<RecentPrediction> RecentPredictions);
    public record RoundStats(string RoundName, int TotalMatches, int Correct, int Wrong, int Missed, double Points);
    public record RecentPrediction(int MatchId, string HomeTeam, string AwayTeam, string? HomeFlag, string? AwayFlag,
        DateTime MatchDate, int? ActualHomeScore, int? ActualAwayScore,
        int? PredictedHomeScore, int? PredictedAwayScore, string? PredictedResult,
        bool? IsCorrect, double PointsEarned, string RoundName);

    // ===== ADMIN STATS =====
    public record AdminStats(int TotalMembers, int TotalMatches, int CompletedMatches,
        int TotalPredictions, double AverageAccuracy, List<LeaderboardEntry> FullLeaderboard);

    // ===== BONUS =====
    public record CreateBonusQuestionRequest(string Question, int BonusPoints, DateTime Deadline);
    public record SubmitBonusAnswerRequest(int QuestionId, string Answer);
    public record ResolveBonusQuestionRequest(string CorrectAnswer);
    public record BonusQuestionDto(int Id, string Question, string? CorrectAnswer, int BonusPoints,
        DateTime Deadline, bool IsResolved, string? MyAnswer);
}
