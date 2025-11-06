using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using rezapAPI.Data;
using rezapAPI.Model;

namespace rezapAPI.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InvitesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public InvitesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<object>>> GetPending()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email)) return Ok(Enumerable.Empty<object>());

            var now = DateTime.UtcNow;
            var invites = await _context.TeamInvites
                .Include(i => i.Team)
                .Where(i => i.Email == email && i.Status == InviteStatus.Pending && i.ExpiresAt > now)
                .Select(i => new {
                    id = i.Id,
                    teamId = i.TeamId,
                    teamName = i.Team!.Name,
                    expiresAt = i.ExpiresAt
                }).ToListAsync();

            return Ok(invites);
        }

        public class CreateInviteRequest { public string Email { get; set; } = string.Empty; }

        [HttpPost("teams/{teamId}/invites")]
        public async Task<ActionResult> CreateInvite(Guid teamId, [FromBody] CreateInviteRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest("Email é obrigatório");
            var userId = CurrentUserId();

            // Apenas Owner/Manager podem convidar
            var membership = await _context.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);
            if (membership == null) return NotFound();
            if (membership.BaseRole == TeamBaseRole.Contributor) return Forbid();

            var existing = await _context.TeamInvites.FirstOrDefaultAsync(i => i.TeamId == teamId && i.Email == req.Email && i.Status == InviteStatus.Pending);
            if (existing != null) return Conflict("Convite já existente para este email");

            var invite = new TeamInvite
            {
                TeamId = teamId,
                Email = req.Email,
                InvitedByUserId = userId
            };
            _context.TeamInvites.Add(invite);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPending), new { id = invite.Id }, new { invite.Id });
        }

        [HttpPost("{inviteId}/accept")]
        public async Task<ActionResult> Accept(Guid inviteId)
        {
            var userId = CurrentUserId();
            var email = User.FindFirstValue(ClaimTypes.Email);

            var invite = await _context.TeamInvites.FirstOrDefaultAsync(i => i.Id == inviteId);
            if (invite == null) return NotFound();
            if (invite.Status != InviteStatus.Pending || invite.ExpiresAt <= DateTime.UtcNow) return BadRequest("Convite inválido ou expirado");
            if (!string.Equals(invite.Email, email, StringComparison.OrdinalIgnoreCase)) return Forbid();

            var exists = await _context.TeamMembers.AnyAsync(tm => tm.TeamId == invite.TeamId && tm.UserId == userId);
            if (!exists)
            {
                _context.TeamMembers.Add(new TeamMember
                {
                    TeamId = invite.TeamId,
                    UserId = userId,
                    BaseRole = TeamBaseRole.Contributor,
                    IsActive = true
                });
            }

            invite.Status = InviteStatus.Accepted;
            invite.RespondedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{inviteId}/reject")]
        public async Task<ActionResult> Reject(Guid inviteId)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var invite = await _context.TeamInvites.FirstOrDefaultAsync(i => i.Id == inviteId);
            if (invite == null) return NotFound();
            if (invite.Status != InviteStatus.Pending || invite.ExpiresAt <= DateTime.UtcNow) return BadRequest("Convite inválido ou expirado");
            if (!string.Equals(invite.Email, email, StringComparison.OrdinalIgnoreCase)) return Forbid();

            invite.Status = InviteStatus.Revoked;
            invite.RespondedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
