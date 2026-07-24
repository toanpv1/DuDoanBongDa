using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupPredictor.DTOs;
using WorldCupPredictor.Services;

namespace WorldCupPredictor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng" });

            return Ok(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _authService.ChangePasswordAsync(userId, request);
            if (!result)
                return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var displayName = User.FindFirst(ClaimTypes.GivenName)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { userId, username, displayName, role });
        }
    }
}
