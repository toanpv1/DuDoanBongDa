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
    public class TournamentController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TournamentController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tournaments = await _db.Tournaments
                .Include(t => t.Members)
                .Include(t => t.Matches)
                .Select(t => new TournamentDto(
                    t.Id, t.Name, t.Description, t.PredictionType, t.PointCalculationMethod,
                    t.StartDate, t.EndDate, t.IsActive,
                    t.Members.Count, t.Matches.Count
                ))
                .ToListAsync();
            return Ok(tournaments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var t = await _db.Tournaments
                .Include(t => t.Members).ThenInclude(m => m.User)
                .Include(t => t.Rounds)
                .Include(t => t.Matches)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (t == null) return NotFound();

            return Ok(new
            {
                t.Id,
                t.Name,
                t.Description,
                t.PredictionType,
                t.PointCalculationMethod,
                t.StartDate,
                t.EndDate,
                t.IsActive,
                Members = t.Members.Select(m => new
                {
                    m.Id,
                    m.UserId,
                    UserDisplayName = m.User?.DisplayName,
                    m.JoinedAt,
                    m.MissedMatchPolicy,
                    m.MissedMatchPercentage
                }),
                Rounds = t.Rounds.OrderBy(r => r.SortOrder).Select(r => new RoundDto(
                    r.Id, r.Name, r.ShortName, r.PointsForCorrect, r.SortOrder,
                    t.Matches.Count(m => m.RoundId == r.Id)
                )),
                MatchCount = t.Matches.Count
            });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTournamentRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var tournament = new Tournament
            {
                Name = request.Name,
                Description = request.Description,
                PredictionType = request.PredictionType,
                PointCalculationMethod = request.PointCalculationMethod ?? "Standard",
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = true,
                CreatedBy = userId
            };

            _db.Tournaments.Add(tournament);
            await _db.SaveChangesAsync();

            // Create default World Cup rounds
            var defaultRounds = new[]
            {
                new Round { TournamentId = tournament.Id, Name = "Vòng bảng", ShortName = "GROUP", PointsForCorrect = 1, SortOrder = 1 },
                new Round { TournamentId = tournament.Id, Name = "Vòng 32 đội", ShortName = "R32", PointsForCorrect = 2, SortOrder = 2 },
                new Round { TournamentId = tournament.Id, Name = "Vòng 16 đội", ShortName = "R16", PointsForCorrect = 3, SortOrder = 3 },
                new Round { TournamentId = tournament.Id, Name = "Tứ kết", ShortName = "QF", PointsForCorrect = 5, SortOrder = 4 },
                new Round { TournamentId = tournament.Id, Name = "Bán kết", ShortName = "SF", PointsForCorrect = 7, SortOrder = 5 },
                new Round { TournamentId = tournament.Id, Name = "Tranh hạng 3", ShortName = "THIRD", PointsForCorrect = 10, SortOrder = 6 },
                new Round { TournamentId = tournament.Id, Name = "Chung kết", ShortName = "FINAL", PointsForCorrect = 10, SortOrder = 7 },
            };

            _db.Rounds.AddRange(defaultRounds);
            await _db.SaveChangesAsync();

            return Ok(new { tournament.Id, message = "Tạo giải đấu thành công" });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTournamentRequest request)
        {
            var tournament = await _db.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound();

            if (request.Name != null) tournament.Name = request.Name;
            if (request.Description != null) tournament.Description = request.Description;
            if (request.PredictionType != null) tournament.PredictionType = request.PredictionType;
            if (request.PointCalculationMethod != null) tournament.PointCalculationMethod = request.PointCalculationMethod;
            if (request.StartDate != null) tournament.StartDate = request.StartDate.Value;
            if (request.EndDate != null) tournament.EndDate = request.EndDate.Value;
            if (request.IsActive != null) tournament.IsActive = request.IsActive.Value;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật giải đấu thành công" });
        }

        // ===== MEMBERS =====

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberRequest request)
        {
            var tournament = await _db.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound();

            if (!tournament.IsActive)
                return BadRequest(new { message = "Giải đấu đã đóng, không thể thêm thành viên" });

            if (tournament.EndDate < DateTime.UtcNow)
                return BadRequest(new { message = "Giải đấu đã kết thúc, không thể thêm thành viên" });

            if (await _db.TournamentMembers.AnyAsync(tm => tm.TournamentId == id && tm.UserId == request.UserId))
                return BadRequest(new { message = "Thành viên đã có trong giải đấu" });

            var member = new TournamentMember
            {
                TournamentId = id,
                UserId = request.UserId,
                MissedMatchPolicy = request.MissedMatchPolicy,
                MissedMatchPercentage = request.MissedMatchPercentage
            };

            _db.TournamentMembers.Add(member);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Thêm thành viên thành công" });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            var tournament = await _db.Tournaments.FindAsync(id);
            if (tournament == null) return NotFound();

            if (!tournament.IsActive)
                return BadRequest(new { message = "Giải đấu đã đóng, không thể xóa thành viên" });

            if (tournament.EndDate < DateTime.UtcNow)
                return BadRequest(new { message = "Giải đấu đã kết thúc, không thể xóa thành viên" });

            var member = await _db.TournamentMembers
                .FirstOrDefaultAsync(tm => tm.TournamentId == id && tm.UserId == userId);
            if (member == null) return NotFound();

            _db.TournamentMembers.Remove(member);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Xóa thành viên thành công" });
        }

        // ===== ROUNDS =====

        [HttpGet("{id}/rounds")]
        public async Task<IActionResult> GetRounds(int id)
        {
            var rounds = await _db.Rounds
                .Where(r => r.TournamentId == id)
                .OrderBy(r => r.SortOrder)
                .Select(r => new RoundDto(
                    r.Id, r.Name, r.ShortName, r.PointsForCorrect, r.SortOrder,
                    r.Matches.Count
                ))
                .ToListAsync();
            return Ok(rounds);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("{id}/rounds")]
        public async Task<IActionResult> CreateRound(int id, [FromBody] CreateRoundRequest request)
        {
            var round = new Round
            {
                TournamentId = id,
                Name = request.Name,
                ShortName = request.ShortName,
                PointsForCorrect = request.PointsForCorrect,
                SortOrder = request.SortOrder
            };

            _db.Rounds.Add(round);
            await _db.SaveChangesAsync();
            return Ok(new { round.Id, message = "Tạo vòng đấu thành công" });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id}/rounds/{roundId}")]
        public async Task<IActionResult> UpdateRound(int id, int roundId, [FromBody] UpdateRoundRequest request)
        {
            var round = await _db.Rounds.FirstOrDefaultAsync(r => r.Id == roundId && r.TournamentId == id);
            if (round == null) return NotFound();

            if (request.Name != null) round.Name = request.Name;
            if (request.ShortName != null) round.ShortName = request.ShortName;
            if (request.PointsForCorrect.HasValue) round.PointsForCorrect = request.PointsForCorrect.Value;
            if (request.SortOrder.HasValue) round.SortOrder = request.SortOrder.Value;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật vòng đấu thành công" });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpDelete("{id}/rounds/{roundId}")]
        public async Task<IActionResult> DeleteRound(int id, int roundId)
        {
            var round = await _db.Rounds
                .Include(r => r.Matches)
                .FirstOrDefaultAsync(r => r.Id == roundId && r.TournamentId == id);
            if (round == null) return NotFound();

            if (round.Matches.Any())
                return BadRequest(new { message = $"Không thể xóa - vòng đấu đã có {round.Matches.Count} trận" });

            _db.Rounds.Remove(round);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Xóa vòng đấu thành công" });
        }
    }
}
