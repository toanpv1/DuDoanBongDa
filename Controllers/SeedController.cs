using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.Data;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SeedController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("init")]
        public async Task<IActionResult> SeedAll()
        {
            if (await _db.Tournaments.AnyAsync())
                return BadRequest(new { message = "Dữ liệu đã tồn tại" });

            // 1. Create Tournament
            var tournament = new Tournament
            {
                Name = "FIFA World Cup 2026",
                Description = "Giải vô địch bóng đá thế giới 2026 - Mỹ, Canada, Mexico",
                PredictionType = "Score",
                StartDate = new DateTime(2026, 6, 11, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                CreatedBy = 1
            };
            _db.Tournaments.Add(tournament);
            await _db.SaveChangesAsync();

            // 2. Create Rounds
            var rounds = new Dictionary<string, Round>();
            var roundData = new[]
            {
                ("Vòng bảng", "GROUP", 1, 1),
                ("Vòng 32 đội", "R32", 2, 2),
                ("Vòng 16 đội", "R16", 3, 3),
                ("Tứ kết", "QF", 5, 4),
                ("Bán kết", "SF", 7, 5),
                ("Tranh hạng 3", "THIRD", 10, 6),
                ("Chung kết", "FINAL", 10, 7)
            };
            foreach (var (name, shortName, pts, order) in roundData)
            {
                var r = new Round { TournamentId = tournament.Id, Name = name, ShortName = shortName, PointsForCorrect = pts, SortOrder = order };
                _db.Rounds.Add(r);
                rounds[shortName] = r;
            }
            await _db.SaveChangesAsync();

            var groupRound = rounds["GROUP"];
            var tid = tournament.Id;
            var rid = groupRound.Id;

            // 3. Create sample users
            var users = new[]
            {
                new User { Username = "user1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), DisplayName = "Nguyễn Văn An", Role = "Member" },
                new User { Username = "user2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), DisplayName = "Trần Thị Bình", Role = "Member" },
                new User { Username = "user3", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), DisplayName = "Lê Hoàng Cường", Role = "Member" },
                new User { Username = "user4", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), DisplayName = "Phạm Minh Đức", Role = "Member" },
                new User { Username = "user5", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"), DisplayName = "Hoàng Thu Hà", Role = "Member" },
            };
            _db.Users.AddRange(users);
            await _db.SaveChangesAsync();

            // 4. Add all users as tournament members (including admin)
            var allUserIds = await _db.Users.Select(u => u.Id).ToListAsync();
            foreach (var uid in allUserIds)
            {
                _db.TournamentMembers.Add(new TournamentMember
                {
                    TournamentId = tid, UserId = uid,
                    MissedMatchPolicy = "AllWrong", MissedMatchPercentage = 0
                });
            }
            await _db.SaveChangesAsync();

            // 5. SEED MATCHES - Lượt 1 vòng bảng (12-18/6)
            var matches = new List<(string grp, string home, string away, string hf, string af, DateTime date, int? hs, int? aws, string status)>
            {
                // === LƯỢT 1 ===
                // Bảng A
                ("A","Mexico","Nam Phi","🇲🇽","🇿🇦", D(12,6,2), 2, 0, "Completed"),
                ("A","Hàn Quốc","CH Séc","🇰🇷","🇨🇿", D(12,6,8), 2, 1, "Completed"),
                // Bảng B
                ("B","Canada","Bosnia & Herzegovina","🇨🇦","🇧🇦", D(13,6,2), 1, 1, "Completed"),
                ("B","Qatar","Thụy Sĩ","🇶🇦","🇨🇭", D(14,6,2), 1, 1, "Completed"),
                // Bảng C
                ("C","Brazil","Ma Rốc","🇧🇷","🇲🇦", D(14,6,5), 1, 1, "Completed"),
                ("C","Haiti","Scotland","🇭🇹","🏴󠁧󠁢󠁳󠁣󠁴󠁿", D(14,6,5), 0, 1, "Completed"),
                // Bảng D
                ("D","Mỹ","Paraguay","🇺🇸","🇵🇾", D(13,6,5), 4, 1, "Completed"),
                ("D","Úc","Thổ Nhĩ Kỳ","🇦🇺","🇹🇷", D(14,6,8), 2, 0, "Completed"),
                // Bảng E
                ("E","Đức","Curacao","🇩🇪","🇨🇼", D(15,6,2), 7, 1, "Completed"),
                ("E","Bờ Biển Ngà","Ecuador","🇨🇮","🇪🇨", D(15,6,5), 1, 0, "Completed"),
                // Bảng F
                ("F","Hà Lan","Nhật Bản","🇳🇱","🇯🇵", D(15,6,8), 2, 2, "Completed"),
                ("F","Thụy Điển","Tunisia","🇸🇪","🇹🇳", D(15,6,11), 5, 1, "Completed"),
                // Bảng G
                ("G","Bỉ","Ai Cập","🇧🇪","🇪🇬", D(16,6,2), 1, 1, "Completed"),
                ("G","Iran","New Zealand","🇮🇷","🇳🇿", D(16,6,5), 2, 2, "Completed"),
                // Bảng H
                ("H","Tây Ban Nha","Cabo Verde","🇪🇸","🇨🇻", D(15,6,20), 0, 0, "Completed"),
                ("H","Ả Rập Saudi","Uruguay","🇸🇦","🇺🇾", D(16,6,8), 1, 1, "Completed"),
                // Bảng I
                ("I","Pháp","Senegal","🇫🇷","🇸🇳", D(17,6,2), 3, 1, "Completed"),
                ("I","Iraq","Na Uy","🇮🇶","🇳🇴", D(17,6,5), 1, 4, "Completed"),
                // Bảng J
                ("J","Argentina","Algeria","🇦🇷","🇩🇿", D(17,6,8), 3, 0, "Completed"),
                ("J","Áo","Jordan","🇦🇹","🇯🇴", D(17,6,11), 3, 1, "Completed"),
                // Bảng K
                ("K","Bồ Đào Nha","CHDC Congo","🇵🇹","🇨🇩", D(18,6,2), 1, 1, "Completed"),
                ("K","Uzbekistan","Colombia","🇺🇿","🇨🇴", D(18,6,5), 1, 3, "Completed"),
                // Bảng L
                ("L","Anh","Croatia","🏴󠁧󠁢󠁥󠁮󠁧󠁿","🇭🇷", D(18,6,8), 4, 2, "Completed"),
                ("L","Ghana","Panama","🇬🇭","🇵🇦", D(18,6,11), 1, 0, "Completed"),

                // === LƯỢT 2 ===
                // Bảng A
                ("A","CH Séc","Nam Phi","🇨🇿","🇿🇦", D(18,6,20), 1, 1, "Completed"),
                ("A","Mexico","Hàn Quốc","🇲🇽","🇰🇷", D(19,6,2), 1, 0, "Completed"),
                // Bảng B
                ("B","Thụy Sĩ","Bosnia & Herzegovina","🇨🇭","🇧🇦", D(19,6,5), 4, 1, "Completed"),
                ("B","Canada","Qatar","🇨🇦","🇶🇦", D(19,6,8), 6, 0, "Completed"),
                // Bảng C
                ("C","Scotland","Ma Rốc","🏴󠁧󠁢󠁳󠁣󠁴󠁿","🇲🇦", D(20,6,2), 0, 1, "Completed"),
                ("C","Brazil","Haiti","🇧🇷","🇭🇹", D(20,6,5), 3, 0, "Completed"),
                // Bảng D
                ("D","Mỹ","Úc","🇺🇸","🇦🇺", D(20,6,8), 2, 0, "Completed"),
                ("D","Thổ Nhĩ Kỳ","Paraguay","🇹🇷","🇵🇾", D(20,6,11), 0, 1, "Completed"),
                // Bảng E
                ("E","Đức","Bờ Biển Ngà","🇩🇪","🇨🇮", D(21,6,2), 2, 1, "Completed"),
                ("E","Ecuador","Curacao","🇪🇨","🇨🇼", D(21,6,5), 0, 0, "Completed"),
                // Bảng F
                ("F","Hà Lan","Thụy Điển","🇳🇱","🇸🇪", D(21,6,8), 5, 1, "Completed"),
                ("F","Tunisia","Nhật Bản","🇹🇳","🇯🇵", D(21,6,11), 0, 4, "Completed"),
                // Bảng H
                ("H","Tây Ban Nha","Ả Rập Saudi","🇪🇸","🇸🇦", D(21,6,20), 4, 0, "Completed"),
                // Bảng G
                ("G","Bỉ","Iran","🇧🇪","🇮🇷", D(22,6,2), 0, 0, "Completed"),
                ("G","New Zealand","Ai Cập","🇳🇿","🇪🇬", D(22,6,5), 1, 3, "Completed"),
                // Bảng H
                ("H","Uruguay","Cabo Verde","🇺🇾","🇨🇻", D(22,6,8), 2, 2, "Completed"),
                // Bảng I
                ("I","Pháp","Iraq","🇫🇷","🇮🇶", D(23,6,2), 3, 0, "Completed"),
                ("I","Na Uy","Senegal","🇳🇴","🇸🇳", D(23,6,5), 3, 2, "Completed"),
                // Bảng J
                ("J","Argentina","Áo","🇦🇷","🇦🇹", D(23,6,0), 2, 0, "Completed"),
                ("J","Jordan","Algeria","🇯🇴","🇩🇿", D(23,6,23), 1, 2, "Completed"),
                // Bảng K
                ("K","Bồ Đào Nha","Uzbekistan","🇵🇹","🇺🇿", D(24,6,0), 5, 0, "Completed"),
                ("K","Colombia","CHDC Congo","🇨🇴","🇨🇩", D(24,6,9), null, null, "Live"),
                // Bảng L
                ("L","Anh","Ghana","🏴󠁧󠁢󠁥󠁮󠁧󠁿","🇬🇭", D(24,6,3), 0, 0, "Completed"),
                ("L","Panama","Croatia","🇵🇦","🇭🇷", D(24,6,6), 0, 1, "Completed"),

                // === LƯỢT 3 (sắp tới) ===
                // Bảng B
                ("B","Thụy Sĩ","Canada","🇨🇭","🇨🇦", D(25,6,2), null, null, "Scheduled"),
                ("B","Bosnia & Herzegovina","Qatar","🇧🇦","🇶🇦", D(25,6,2), null, null, "Scheduled"),
                // Bảng C
                ("C","Scotland","Brazil","🏴󠁧󠁢󠁳󠁣󠁴󠁿","🇧🇷", D(25,6,5), null, null, "Scheduled"),
                ("C","Ma Rốc","Haiti","🇲🇦","🇭🇹", D(25,6,5), null, null, "Scheduled"),
                // Bảng A
                ("A","Nam Phi","Hàn Quốc","🇿🇦","🇰🇷", D(25,6,8), null, null, "Scheduled"),
                ("A","CH Séc","Mexico","🇨🇿","🇲🇽", D(25,6,8), null, null, "Scheduled"),
                // Bảng E
                ("E","Ecuador","Đức","🇪🇨","🇩🇪", D(26,6,3), null, null, "Scheduled"),
                ("E","Curacao","Bờ Biển Ngà","🇨🇼","🇨🇮", D(26,6,3), null, null, "Scheduled"),
                // Bảng F
                ("F","Tunisia","Hà Lan","🇹🇳","🇳🇱", D(26,6,6), null, null, "Scheduled"),
                ("F","Nhật Bản","Thụy Điển","🇯🇵","🇸🇪", D(26,6,6), null, null, "Scheduled"),
                // Bảng D
                ("D","Thổ Nhĩ Kỳ","Mỹ","🇹🇷","🇺🇸", D(26,6,9), null, null, "Scheduled"),
                ("D","Paraguay","Úc","🇵🇾","🇦🇺", D(26,6,9), null, null, "Scheduled"),
                // Bảng I
                ("I","Na Uy","Pháp","🇳🇴","🇫🇷", D(27,6,2), null, null, "Scheduled"),
                ("I","Senegal","Iraq","🇸🇳","🇮🇶", D(27,6,2), null, null, "Scheduled"),
                // Bảng H
                ("H","Uruguay","Tây Ban Nha","🇺🇾","🇪🇸", D(27,6,7), null, null, "Scheduled"),
                ("H","Cabo Verde","Ả Rập Saudi","🇨🇻","🇸🇦", D(27,6,7), null, null, "Scheduled"),
                // Bảng G
                ("G","New Zealand","Bỉ","🇳🇿","🇧🇪", D(27,6,10), null, null, "Scheduled"),
                ("G","Ai Cập","Iran","🇪🇬","🇮🇷", D(27,6,10), null, null, "Scheduled"),
                // Bảng L
                ("L","Panama","Anh","🇵🇦","🏴󠁧󠁢󠁥󠁮󠁧󠁿", D(28,6,4), null, null, "Scheduled"),
                ("L","Croatia","Ghana","🇭🇷","🇬🇭", D(28,6,4), null, null, "Scheduled"),
                // Bảng K
                ("K","Uzbekistan","Bồ Đào Nha","🇺🇿","🇵🇹", D(28,6,7), null, null, "Scheduled"),
                ("K","CHDC Congo","Colombia","🇨🇩","🇨🇴", D(28,6,7), null, null, "Scheduled"),
                // Bảng J
                ("J","Algeria","Argentina","🇩🇿","🇦🇷", D(28,6,10), null, null, "Scheduled"),
                ("J","Jordan","Áo","🇯🇴","🇦🇹", D(28,6,10), null, null, "Scheduled"),
            };

            foreach (var m in matches)
            {
                _db.Matches.Add(new Match
                {
                    TournamentId = tid, RoundId = rid, GroupName = m.grp,
                    HomeTeam = m.home, AwayTeam = m.away,
                    HomeFlag = m.hf, AwayFlag = m.af,
                    MatchDate = m.date, PredictionDeadline = m.date.AddMinutes(-30),
                    HomeScore = m.hs, AwayScore = m.aws, Status = m.status
                });
            }
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Seed dữ liệu thành công!",
                tournament = tournament.Name,
                rounds = roundData.Length,
                matches = matches.Count,
                users = users.Length + 1
            });
        }

        // Helper: Create UTC DateTime for Vietnam timezone (UTC+7)
        private static DateTime D(int day, int month, int hourVN)
        {
            // Convert Vietnam time (UTC+7) to UTC
            return new DateTime(2026, month, day, hourVN, 0, 0, DateTimeKind.Utc).AddHours(-7);
        }
    }
}
