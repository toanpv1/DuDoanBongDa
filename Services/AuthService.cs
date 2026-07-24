using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WorldCupPredictor.Data;
using WorldCupPredictor.DTOs;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Username == request.Username && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;

            var token = GenerateJwtToken(user);
            return new LoginResponse(token, user.DisplayName, user.Role, user.Id);
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _db.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _config["Jwt:Key"] ?? "WorldCupPredictor2026SuperSecretKey!@#$%"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "WorldCupPredictor",
                audience: _config["Jwt:Audience"] ?? "WorldCupPredictor",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
