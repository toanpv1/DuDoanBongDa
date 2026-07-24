using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.Data;
using WorldCupPredictor.DTOs;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PredictionController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PredictionController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitPredictionRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var match = await _db.Matches.FindAsync(request.MatchId);
            if (match == null) return NotFound(new { message = "Trận đấu không tồn tại" });

            if (!match.IsPredictionOpen)
                return BadRequest(new { message = "Đã hết thời gian dự đoán cho trận này" });

            // Check if user is member of tournament
            var isMember = await _db.TournamentMembers
                .AnyAsync(tm => tm.TournamentId == match.TournamentId && tm.UserId == userId);
            if (!isMember)
                return BadRequest(new { message = "Bạn không phải thành viên của giải đấu này" });

            var existing = await _db.Predictions
                .FirstOrDefaultAsync(p => p.MatchId == request.MatchId && p.UserId == userId);

            if (existing != null)
            {
                existing.PredictedHomeScore = request.PredictedHomeScore;
                existing.PredictedAwayScore = request.PredictedAwayScore;
                existing.PredictedResult = request.PredictedResult;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var prediction = new Prediction
                {
                    MatchId = request.MatchId,
                    UserId = userId,
                    PredictedHomeScore = request.PredictedHomeScore,
                    PredictedAwayScore = request.PredictedAwayScore,
                    PredictedResult = request.PredictedResult
                };
                _db.Predictions.Add(prediction);
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Dự đoán thành công" });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyPredictions([FromQuery] int tournamentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var predictions = await _db.Predictions
                .Include(p => p.Match).ThenInclude(m => m!.Round)
                .Where(p => p.UserId == userId && p.Match!.TournamentId == tournamentId)
                .OrderByDescending(p => p.Match!.MatchDate)
                .Select(p => new RecentPrediction(
                    p.MatchId, p.Match!.HomeTeam, p.Match.AwayTeam, p.Match.HomeFlag, p.Match.AwayFlag,
                    p.Match.MatchDate, p.Match.HomeScore, p.Match.AwayScore,
                    p.PredictedHomeScore, p.PredictedAwayScore, p.PredictedResult,
                    p.IsCorrect, p.PointsEarned, p.Match.Round!.Name
                ))
                .ToListAsync();

            return Ok(predictions);
        }
    }
}
