using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using rezapAPI.Data;
using rezapAPI.Model;
using System.Security.Claims;
using System.Threading.Tasks;

namespace rezapAPI.Controller
{
    [ApiController]
    [Route("api/teams/{teamId}/audit")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(Guid teamId, [FromQuery] int limit = 50)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Verifica se o usuário é membro do time
            var isMember = await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);

            if (!isMember)
                return Forbid();

            var logs = await _context.TeamAuditLogs
                .Where(a => a.TeamId == teamId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(Math.Min(limit, 200))
                .Include(a => a.User)
                .Select(a => new
                {
                    a.Id,
                    a.Action,
                    a.EntityId,
                    a.Details,
                    a.CreatedAt,
                    User = new
                    {
                        Id = a.UserId,
                        Email = a.User != null ? a.User.Email : "Unknown"
                    }
                })
                .ToListAsync();

            return Ok(logs);
        }
    }

    // Helper service para registrar ações de auditoria
    public interface IAuditService
    {
        System.Threading.Tasks.Task LogAsync(Guid teamId, string userId, AuditAction action, string? entityId = null, string? details = null);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task LogAsync(Guid teamId, string userId, AuditAction action, string? entityId = null, string? details = null)
        {
            var log = new TeamAuditLog
            {
                TeamId = teamId,
                UserId = userId,
                Action = action,
                EntityId = entityId,
                Details = details
            };

            _context.TeamAuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
