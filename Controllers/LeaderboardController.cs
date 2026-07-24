using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupPredictor.DTOs;
using WorldCupPredictor.Services;

namespace WorldCupPredictor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaderboardController : ControllerBase
    {
        private readonly ScoringService _scoringService;

        public LeaderboardController(ScoringService scoringService)
        {
            _scoringService = scoringService;
        }

        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> Get(int tournamentId)
        {
            var leaderboard = await _scoringService.GetLeaderboardAsync(tournamentId);
            return Ok(leaderboard);
        }

        [HttpGet("my-stats/{tournamentId}")]
        public async Task<IActionResult> GetMyStats(int tournamentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var stats = await _scoringService.GetPersonalStatsAsync(userId, tournamentId);
            return Ok(stats);
        }
    }
}
