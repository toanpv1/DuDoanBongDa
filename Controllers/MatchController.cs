using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.Data;
using WorldCupPredictor.DTOs;
using WorldCupPredictor.Models;
using WorldCupPredictor.Services;

namespace WorldCupPredictor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ScoringService _scoringService;

        public MatchController(AppDbContext db, ScoringService scoringService)
        {
            _db = db;
            _scoringService = scoringService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int tournamentId, [FromQuery] int? roundId, [FromQuery] string? status)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var tournament = await _db.Tournaments.FindAsync(tournamentId);
            var predictionType = tournament?.PredictionType ?? "Score";

            var query = _db.Matches.Include(m => m.Round).Where(m => m.TournamentId == tournamentId);
            if (roundId.HasValue) query = query.Where(m => m.RoundId == roundId.Value);
            if (!string.IsNullOrEmpty(status)) query = query.Where(m => m.Status == status);

            var matches = await query.OrderBy(m => m.MatchDate).ToListAsync();
            var matchIds = matches.Select(m => m.Id).ToList();
            var myPredictions = await _db.Predictions
                .Where(p => p.UserId == userId && matchIds.Contains(p.MatchId))
                .ToDictionaryAsync(p => p.MatchId);

            var result = matches.Select(m =>
            {
                MyPredictionDto? myPred = null;
                if (myPredictions.TryGetValue(m.Id, out var pred))
                    myPred = new MyPredictionDto(pred.Id, pred.PredictedHomeScore, pred.PredictedAwayScore, pred.PredictedResult, pred.PointsEarned, pred.IsCorrect);

                return new MatchDto(m.Id, m.TournamentId, m.RoundId, m.Round?.Name ?? "", m.GroupName,
                    m.HomeTeam, m.AwayTeam, m.HomeFlag, m.AwayFlag, m.MatchDate, m.PredictionDeadline,
                    m.HomeScore, m.AwayScore, m.Status, m.IsPredictionOpen, m.Round?.PointsForCorrect ?? 1, predictionType, myPred);
            });
            return Ok(result);
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcoming([FromQuery] int tournamentId, [FromQuery] int count = 5)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var tournament = await _db.Tournaments.FindAsync(tournamentId);
            var predictionType = tournament?.PredictionType ?? "Score";

            var matches = await _db.Matches.Include(m => m.Round)
                .Where(m => m.TournamentId == tournamentId && m.Status == "Scheduled" && m.MatchDate > DateTime.UtcNow)
                .OrderBy(m => m.MatchDate).Take(count).ToListAsync();

            var matchIds = matches.Select(m => m.Id).ToList();
            var myPredictions = await _db.Predictions
                .Where(p => p.UserId == userId && matchIds.Contains(p.MatchId))
                .ToDictionaryAsync(p => p.MatchId);

            var result = matches.Select(m =>
            {
                MyPredictionDto? myPred = null;
                if (myPredictions.TryGetValue(m.Id, out var pred))
                    myPred = new MyPredictionDto(pred.Id, pred.PredictedHomeScore, pred.PredictedAwayScore, pred.PredictedResult, pred.PointsEarned, pred.IsCorrect);
                return new MatchDto(m.Id, m.TournamentId, m.RoundId, m.Round?.Name ?? "", m.GroupName,
                    m.HomeTeam, m.AwayTeam, m.HomeFlag, m.AwayFlag, m.MatchDate, m.PredictionDeadline,
                    m.HomeScore, m.AwayScore, m.Status, m.IsPredictionOpen, m.Round?.PointsForCorrect ?? 1, predictionType, myPred);
            });
            return Ok(result);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMatchRequest request)
        {
            var match = new Match
            {
                TournamentId = request.TournamentId, RoundId = request.RoundId, GroupName = request.GroupName,
                HomeTeam = request.HomeTeam, AwayTeam = request.AwayTeam,
                HomeFlag = request.HomeFlag, AwayFlag = request.AwayFlag,
                MatchDate = request.MatchDate, PredictionDeadline = request.MatchDate.AddMinutes(-30), Status = "Scheduled"
            };
            _db.Matches.Add(match);
            await _db.SaveChangesAsync();
            return Ok(new { match.Id, message = "Tạo trận đấu thành công" });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("batch")]
        public async Task<IActionResult> CreateBatch([FromBody] List<CreateMatchRequest> requests)
        {
            var matches = requests.Select(r => new Match
            {
                TournamentId = r.TournamentId, RoundId = r.RoundId, GroupName = r.GroupName,
                HomeTeam = r.HomeTeam, AwayTeam = r.AwayTeam, HomeFlag = r.HomeFlag, AwayFlag = r.AwayFlag,
                MatchDate = r.MatchDate, PredictionDeadline = r.MatchDate.AddMinutes(-30), Status = "Scheduled"
            }).ToList();
            _db.Matches.AddRange(matches);
            await _db.SaveChangesAsync();
            return Ok(new { count = matches.Count, message = $"Tạo {matches.Count} trận đấu thành công" });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id}/result")]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] UpdateMatchResultRequest request)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound();
            match.HomeScore = request.HomeScore;
            match.AwayScore = request.AwayScore;
            match.Status = "Completed";
            await _db.SaveChangesAsync();
            await _scoringService.CalculateMatchScoresAsync(id);
            return Ok(new { message = "Cập nhật kết quả và tính điểm thành công" });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound();
            var predictions = await _db.Predictions.Where(p => p.MatchId == id).ToListAsync();
            _db.Predictions.RemoveRange(predictions);
            _db.Matches.Remove(match);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Xóa trận đấu thành công" });
        }

        [HttpGet("{id}/stats")]
        public async Task<IActionResult> GetMatchStats(int id)
        {
            var match = await _db.Matches.FindAsync(id);
            if (match == null) return NotFound();

            var predictions = await _db.Predictions
                .Include(p => p.User)
                .Where(p => p.MatchId == id)
                .Select(p => new MatchPredictionStatDto(
                    p.UserId,
                    p.User!.DisplayName,
                    p.PredictedHomeScore,
                    p.PredictedAwayScore,
                    p.PredictedResult,
                    p.IsCorrect,
                    p.PointsEarned
                ))
                .ToListAsync();

            var correctCount = predictions.Count(p => p.IsCorrect == true);
            var wrongCount = predictions.Count(p => p.IsCorrect == false);

            return Ok(new MatchStatsDto(id, predictions.Count, correctCount, wrongCount, predictions));
        }
    }
}
