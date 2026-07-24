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
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly AuthService _authService;
        private readonly ScoringService _scoringService;

        public AdminController(AppDbContext db, AuthService authService, ScoringService scoringService)
        {
            _db = db;
            _authService = authService;
            _scoringService = scoringService;
        }

        // ===== USER MANAGEMENT =====

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _db.Users
                .Select(u => new UserDto(u.Id, u.Username, u.DisplayName, u.Email, u.Role, u.IsActive, u.CreatedAt))
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (await _db.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại" });

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                DisplayName = request.DisplayName,
                Email = request.Email,
                Role = request.Role,
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new UserDto(user.Id, user.Username, user.DisplayName, user.Email, user.Role, user.IsActive, user.CreatedAt));
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (request.DisplayName != null) user.DisplayName = request.DisplayName;
            if (request.Email != null) user.Email = request.Email;
            if (request.Role != null) user.Role = request.Role;
            if (request.IsActive != null) user.IsActive = request.IsActive.Value;

            await _db.SaveChangesAsync();
            return Ok(new UserDto(user.Id, user.Username, user.DisplayName, user.Email, user.Role, user.IsActive, user.CreatedAt));
        }

        [HttpPost("users/{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(id, request.NewPassword);
            if (!result) return NotFound();
            return Ok(new { message = "Reset mật khẩu thành công" });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false; // Soft delete
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã vô hiệu hóa user" });
        }

        // ===== ADMIN STATISTICS =====

        [HttpGet("statistics/{tournamentId}")]
        public async Task<IActionResult> GetStatistics(int tournamentId)
        {
            var memberCount = await _db.TournamentMembers.CountAsync(tm => tm.TournamentId == tournamentId);
            var totalMatches = await _db.Matches.CountAsync(m => m.TournamentId == tournamentId);
            var completedMatches = await _db.Matches.CountAsync(m => m.TournamentId == tournamentId && m.Status == "Completed");
            var totalPredictions = await _db.Predictions
                .CountAsync(p => p.Match!.TournamentId == tournamentId);

            var leaderboard = await _scoringService.GetLeaderboardAsync(tournamentId);
            var avgAccuracy = leaderboard.Any() ? leaderboard.Average(l => l.Accuracy) : 0;

            return Ok(new AdminStats(
                memberCount, totalMatches, completedMatches,
                totalPredictions, Math.Round(avgAccuracy, 1),
                leaderboard
            ));
        }

        // ===== VIEW ALL PREDICTIONS (Admin only) =====

        [HttpGet("predictions/{tournamentId}")]
        public async Task<IActionResult> GetAllPredictions(int tournamentId, [FromQuery] int? matchId)
        {
            var query = _db.Predictions
                .Include(p => p.User)
                .Include(p => p.Match)
                .Where(p => p.Match!.TournamentId == tournamentId);

            if (matchId.HasValue)
                query = query.Where(p => p.MatchId == matchId.Value);

            var predictions = await query
                .OrderBy(p => p.Match!.MatchDate)
                .ThenBy(p => p.User!.DisplayName)
                .Select(p => new
                {
                    p.Id,
                    p.MatchId,
                    HomeTeam = p.Match!.HomeTeam,
                    AwayTeam = p.Match.AwayTeam,
                    MatchDate = p.Match.MatchDate,
                    ActualHomeScore = p.Match.HomeScore,
                    ActualAwayScore = p.Match.AwayScore,
                    MatchStatus = p.Match.Status,
                    p.UserId,
                    UserDisplayName = p.User!.DisplayName,
                    p.PredictedHomeScore,
                    p.PredictedAwayScore,
                    p.PredictedResult,
                    p.PointsEarned,
                    p.IsCorrect
                })
                .ToListAsync();

            return Ok(predictions);
        }
        // ===== FETCH DATA (Scraping API) =====
        
        public class FetchMatchesRequest
        {
            public int TournamentId { get; set; }
            public int RoundId { get; set; }
            public string Date { get; set; } = string.Empty;
            public string Keyword { get; set; } = string.Empty;
        }

        [HttpPost("fetch-matches")]
        public async Task<IActionResult> FetchMatches([FromBody] FetchMatchesRequest req)
        {
            if (!DateTime.TryParse(req.Date, out var date))
                return BadRequest(new { message = "Ngày không hợp lệ" });

            var dateStr = date.ToString("yyyyMMdd");
            var url = $"https://prod-public-api.livescore.com/v1/api/app/date/soccer/{dateStr}/7?MD=1";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            
            try 
            {
                var response = await client.GetStringAsync(url);
                using var doc = System.Text.Json.JsonDocument.Parse(response);
                var root = doc.RootElement;
                
                if (!root.TryGetProperty("Stages", out var stages) || stages.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    return BadRequest(new { message = "Không tìm thấy dữ liệu trận đấu trong ngày này." });
                }

                string kwd = req.Keyword?.ToLower().Trim().Replace(" ", "") ?? "";
                if (string.IsNullOrEmpty(kwd)) return BadRequest(new { message = "Vui lòng nhập từ khóa giải đấu (VD: Euro)" });

                int count = 0;
                foreach (var stage in stages.EnumerateArray())
                {
                    var snm = stage.TryGetProperty("Snm", out var sn) ? sn.GetString()?.ToLower().Replace(" ", "") : "";
                    var cnm = stage.TryGetProperty("Cnm", out var cn) ? cn.GetString()?.ToLower().Replace(" ", "") : "";
                    var compn = stage.TryGetProperty("CompN", out var cpn) ? cpn.GetString()?.ToLower().Replace(" ", "") : "";
                    var compd = stage.TryGetProperty("CompD", out var cpd) ? cpd.GetString()?.ToLower().Replace(" ", "") : "";
                    
                    if (!(snm?.Contains(kwd) == true) && !(cnm?.Contains(kwd) == true) && !(compn?.Contains(kwd) == true) && !(compd?.Contains(kwd) == true))
                        continue;

                    if (stage.TryGetProperty("Events", out var events))
                    {
                        foreach (var ev in events.EnumerateArray())
                        {
                            var t1Element = ev.TryGetProperty("T1", out var t1Prop) && t1Prop.ValueKind == System.Text.Json.JsonValueKind.Array ? t1Prop.EnumerateArray().FirstOrDefault() : default;
                            var t2Element = ev.TryGetProperty("T2", out var t2Prop) && t2Prop.ValueKind == System.Text.Json.JsonValueKind.Array ? t2Prop.EnumerateArray().FirstOrDefault() : default;
                            
                            var t1 = t1Element.ValueKind != System.Text.Json.JsonValueKind.Undefined && t1Element.TryGetProperty("Nm", out var t1Nm) ? t1Nm.GetString() : "Đội A";
                            var t2 = t2Element.ValueKind != System.Text.Json.JsonValueKind.Undefined && t2Element.TryGetProperty("Nm", out var t2Nm) ? t2Nm.GetString() : "Đội B";
                            
                            var esdStr = ev.GetProperty("Esd").GetInt64().ToString();
                            DateTime matchDate;
                            if (esdStr.Length == 14)
                            {
                                matchDate = DateTime.ParseExact(esdStr, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();
                            }
                            else
                            {
                                matchDate = date.AddHours(19).ToUniversalTime();
                            }

                            _db.Matches.Add(new Match
                            {
                                TournamentId = req.TournamentId,
                                RoundId = req.RoundId,
                                HomeTeam = t1 ?? "Đội A",
                                AwayTeam = t2 ?? "Đội B",
                                MatchDate = matchDate,
                                PredictionDeadline = matchDate.AddMinutes(-30),
                                Status = "Scheduled"
                            });
                            count++;
                        }
                    }
                }
                
                if (count > 0) await _db.SaveChangesAsync();
                return Ok(new { message = $"Đã tải và thêm {count} trận đấu vào hệ thống." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy dữ liệu: " + ex.Message });
            }
        }
    }
}
