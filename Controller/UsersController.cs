using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using rezapAPI.Model;
using System.Security.Claims;
using System.Text;

namespace rezapAPI.Controller
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // GET /api/users/avatar - Retorna SVG do avatar do usuário logado
        [HttpGet("avatar")]
        [Authorize]
        public async Task<IActionResult> GetMyAvatar()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Se tiver avatar customizado, redireciona
            if (!string.IsNullOrEmpty(user.CustomAvatarUrl))
                return Redirect(user.CustomAvatarUrl);

            // Gera SVG com iniciais
            var svg = GenerateAvatarSvg(user);
            return Content(svg, "image/svg+xml");
        }

        // GET /api/users/{userId}/avatar - Avatar de qualquer usuário (público para membros de equipe)
        [HttpGet("{userId}/avatar")]
        public async Task<IActionResult> GetUserAvatar(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrEmpty(user.CustomAvatarUrl))
                return Redirect(user.CustomAvatarUrl);

            var svg = GenerateAvatarSvg(user);
            return Content(svg, "image/svg+xml");
        }

        // PUT /api/users/avatar - Atualizar avatar customizado
        [HttpPut("avatar")]
        [Authorize]
        public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            user.CustomAvatarUrl = request.AvatarUrl;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Avatar atualizado com sucesso" });
        }

        // GET /api/users/me - Retorna informações do usuário logado
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                customAvatarUrl = user.CustomAvatarUrl,
                avatarUrl = $"/api/users/avatar"
            });
        }

        private static string GenerateAvatarSvg(User user)
        {
            // Gera iniciais do nome ou email
            string initials = GetInitials(user);
            
            // Gera cor baseada no hash do email
            string color = GetColorFromString(user.Email ?? user.Id);

            var svg = $@"<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'>
    <rect width='100' height='100' fill='{color}'/>
    <text x='50' y='50' font-family='Arial, sans-serif' font-size='40' font-weight='600' 
          fill='white' text-anchor='middle' dominant-baseline='central'>{initials}</text>
</svg>";

            return svg;
        }

        private static string GetInitials(User user)
        {
            if (!string.IsNullOrEmpty(user.FullName))
            {
                var parts = user.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
                if (parts.Length == 1 && parts[0].Length > 0)
                    return parts[0][0].ToString().ToUpper();
            }

            if (!string.IsNullOrEmpty(user.Email))
            {
                var emailPart = user.Email.Split('@')[0];
                if (emailPart.Length >= 2)
                    return emailPart.Substring(0, 2).ToUpper();
                if (emailPart.Length == 1)
                    return emailPart[0].ToString().ToUpper();
            }

            return "??";
        }

        private static string GetColorFromString(string input)
        {
            // Gera cor consistente baseada no hash da string
            int hash = 0;
            foreach (char c in input)
                hash = c + ((hash << 5) - hash);

            var colors = new[]
            {
                "#3B82F6", "#8B5CF6", "#EC4899", "#F59E0B",
                "#10B981", "#06B6D4", "#6366F1", "#EF4444",
                "#14B8A6", "#F97316", "#A855F7", "#84CC16"
            };

            return colors[Math.Abs(hash) % colors.Length];
        }
    }

    public class UpdateAvatarRequest
    {
        public string? AvatarUrl { get; set; }
    }
}
