using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.Data;
using WorldCupPredictor.DTOs;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Services
{
    public class ScoringService
    {
        private readonly AppDbContext _db;

        public ScoringService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Calculate scores for all predictions of a completed match.
        /// Supports two modes:
        ///   - Standard:   correct = +pointsForCorrect, wrong = -pointsForCorrect
        ///   - SharedPool: total pool = count * pointsForCorrect,
        ///                 correct share = pool / correctCount,
        ///                 wrong share   = -pool / wrongCount
        /// </summary>
        public async Task CalculateMatchScoresAsync(int matchId)
        {
            var match = await _db.Matches
                .Include(m => m.Round)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null || match.HomeScore == null || match.AwayScore == null)
                return;

            var predictions = await _db.Predictions
                .Where(p => p.MatchId == matchId)
                .ToListAsync();

            if (!predictions.Any()) return;

            var pointsForCorrect = match.Round?.PointsForCorrect ?? 1;
            var actualResult = match.ActualResult;
            var calculationMethod = match.Tournament?.PointCalculationMethod ?? "Standard";

            // First pass: determine correct/wrong
            foreach (var prediction in predictions)
            {
                var predictedResult = prediction.ComputedResult ?? prediction.PredictedResult;
                prediction.IsCorrect = (predictedResult == actualResult);
            }

            int correctCount = predictions.Count(p => p.IsCorrect == true);
            int wrongCount   = predictions.Count(p => p.IsCorrect == false);

            if (calculationMethod == "SharedPool")
            {
                double totalPool = predictions.Count * pointsForCorrect;
                double pointsPerCorrect = correctCount > 0
                    ? Math.Round(totalPool / correctCount, 2)
                    : 0;
                double pointsPerWrong = wrongCount > 0
                    ? Math.Round(-totalPool / wrongCount, 2)
                    : 0;

                foreach (var prediction in predictions)
                {
                    prediction.PointsEarned = prediction.IsCorrect == true
                        ? pointsPerCorrect
                        : pointsPerWrong;
                    prediction.UpdatedAt = DateTime.UtcNow;
                }
            }
            else // Standard: +pts if correct, -pts if wrong
            {
                foreach (var prediction in predictions)
                {
                    prediction.PointsEarned = prediction.IsCorrect == true
                        ? pointsForCorrect
                        : -pointsForCorrect;
                    prediction.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Get leaderboard for a tournament
        /// </summary>
        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int tournamentId)
        {
            var members = await _db.TournamentMembers
                .Include(tm => tm.User)
                .Where(tm => tm.TournamentId == tournamentId)
                .ToListAsync();

            var completedMatches = await _db.Matches
                .Where(m => m.TournamentId == tournamentId && m.Status == "Completed")
                .Select(m => m.Id)
                .ToListAsync();

            var totalCompleted = completedMatches.Count;

            var allPredictions = await _db.Predictions
                .Where(p => completedMatches.Contains(p.MatchId))
                .ToListAsync();

            var entries = new List<LeaderboardEntry>();

            foreach (var member in members)
            {
                if (member.User == null) continue;

                var userPredictions = allPredictions.Where(p => p.UserId == member.UserId).ToList();
                var correct  = userPredictions.Count(p => p.IsCorrect == true);
                var wrong    = userPredictions.Count(p => p.IsCorrect == false);
                var predicted = userPredictions.Count;
                var missed   = totalCompleted - predicted;

                var totalPoints = userPredictions.Sum(p => p.PointsEarned);
                var accuracy    = totalCompleted > 0 ? (double)correct / totalCompleted * 100 : 0;

                entries.Add(new LeaderboardEntry(
                    0, member.UserId, member.User.DisplayName,
                    totalPoints, totalCompleted, correct, wrong, missed,
                    Math.Round(accuracy, 1)
                ));
            }

            entries = entries
                .OrderByDescending(e => e.TotalPoints)
                .ThenByDescending(e => e.Accuracy)
                .ThenByDescending(e => e.CorrectPredictions)
                .Select((e, i) => e with { Rank = i + 1 })
                .ToList();

            return entries;
        }

        /// <summary>
        /// Get personal statistics
        /// </summary>
        public async Task<PersonalStats> GetPersonalStatsAsync(int userId, int tournamentId)
        {
            var completedMatches = await _db.Matches
                .AsNoTracking()
                .Include(m => m.Round)
                .Where(m => m.TournamentId == tournamentId && m.Status == "Completed")
                .ToListAsync();

            var predictions = await _db.Predictions
                .AsNoTracking()
                .Include(p => p.Match)
                    .ThenInclude(m => m!.Round)
                .Where(p => p.UserId == userId && p.Match!.TournamentId == tournamentId)
                .ToListAsync();

            var totalCompleted = completedMatches.Count;
            var correct  = predictions.Count(p => p.IsCorrect == true);
            var wrong    = predictions.Count(p => p.IsCorrect == false);
            var predictedMatchIds = predictions.Select(p => p.MatchId).ToHashSet();
            var missed   = completedMatches.Count(m => !predictedMatchIds.Contains(m.Id));
            var totalPoints = predictions.Sum(p => p.PointsEarned);
            var accuracy = totalCompleted > 0 ? (double)correct / totalCompleted * 100 : 0;

            var rounds = await _db.Rounds
                .AsNoTracking()
                .Where(r => r.TournamentId == tournamentId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync();

            var roundBreakdown = rounds.Select(round =>
            {
                var roundMatches     = completedMatches.Where(m => m.RoundId == round.Id).ToList();
                var roundPredictions = predictions.Where(p => p.Match?.RoundId == round.Id).ToList();
                var roundCorrect     = roundPredictions.Count(p => p.IsCorrect == true);
                var roundWrong       = roundPredictions.Count(p => p.IsCorrect == false);
                var roundMissed      = roundMatches.Count - roundPredictions.Count(p => p.Match?.Status == "Completed");
                var roundPoints      = roundPredictions.Sum(p => p.PointsEarned);

                return new RoundStats(round.Name, roundMatches.Count, roundCorrect, roundWrong, roundMissed, roundPoints);
            }).ToList();

            var recentPredictions = predictions
                .Where(p => p.Match?.Status == "Completed")
                .OrderByDescending(p => p.Match?.MatchDate)
                .Take(10)
                .Select(p => new RecentPrediction(
                    p.MatchId,
                    p.Match!.HomeTeam, p.Match.AwayTeam,
                    p.Match.HomeFlag, p.Match.AwayFlag,
                    p.Match.MatchDate,
                    p.Match.HomeScore, p.Match.AwayScore,
                    p.PredictedHomeScore, p.PredictedAwayScore,
                    p.PredictedResult,
                    p.IsCorrect, p.PointsEarned,
                    p.Match.Round?.Name ?? ""
                ))
                .ToList();

            return new PersonalStats(
                totalPoints, totalCompleted, correct, wrong, missed,
                Math.Round(accuracy, 1),
                roundBreakdown, recentPredictions
            );
        }
    }
}
