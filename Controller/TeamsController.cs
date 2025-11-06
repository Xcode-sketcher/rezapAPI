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
    public class TeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TeamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetMyTeams()
        {
            var userId = CurrentUserId();
            var teams = await _context.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm => tm.UserId == userId && tm.IsActive)
                .Select(tm => new {
                    teamId = tm.TeamId,
                    name = tm.Team!.Name,
                    role = tm.BaseRole.ToString()
                })
                .ToListAsync();
            return Ok(teams);
        }

        public class CreateTeamRequest { public string Name { get; set; } = string.Empty; }

        [HttpPost]
        public async Task<ActionResult<object>> CreateTeam([FromBody] CreateTeamRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Nome é obrigatório");

            var userId = CurrentUserId();

            var team = new Team
            {
                Name = req.Name.Trim(),
                OwnerId = userId,
            };
            _context.Teams.Add(team);

            var membership = new TeamMember
            {
                TeamId = team.Id,
                UserId = userId,
                BaseRole = TeamBaseRole.Owner,
                IsActive = true
            };
            _context.TeamMembers.Add(membership);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTeamById), new { id = team.Id }, new {
                teamId = team.Id,
                team.Name
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTeamById(Guid id)
        {
            var userId = CurrentUserId();
            var membership = await _context.TeamMembers
                .Include(tm => tm.Team)
                .FirstOrDefaultAsync(tm => tm.TeamId == id && tm.UserId == userId && tm.IsActive);
            if (membership == null) return NotFound();

            var members = await _context.TeamMembers
                .Where(tm => tm.TeamId == id)
                .Select(tm => new { tm.UserId, role = tm.BaseRole.ToString(), tm.JoinedAt, tm.IsActive })
                .ToListAsync();

            return Ok(new {
                teamId = id,
                name = membership.Team!.Name,
                role = membership.BaseRole.ToString(),
                members
            });
        }

        // GET /api/teams/{id}/members - Listar membros com detalhes
        [HttpGet("{id}/members")]
        public async Task<ActionResult<IEnumerable<object>>> GetTeamMembers(Guid id)
        {
            var userId = CurrentUserId();
            var membership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == id && tm.UserId == userId && tm.IsActive);
            if (membership == null) return Forbid();

            var members = await _context.TeamMembers
                .Include(tm => tm.User)
                .Include(tm => tm.Grants)
                .Where(tm => tm.TeamId == id && tm.IsActive)
                .Select(tm => new {
                    id = tm.Id,
                    userId = tm.UserId,
                    email = tm.User!.Email,
                    fullName = tm.User.FullName,
                    role = tm.BaseRole.ToString(),
                    joinedAt = tm.JoinedAt,
                    grants = tm.Grants.Select(g => new { g.Scope, grantType = g.GrantType.ToString() })
                })
                .ToListAsync();

            return Ok(members);
        }

        // DELETE /api/teams/{teamId}/members/{memberId} - Remover membro (Owner ou Manager)
        [HttpDelete("{teamId}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(Guid teamId, Guid memberId)
        {
            var userId = CurrentUserId();
            var myMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);

            if (myMembership == null) return Forbid();
            if (myMembership.BaseRole != TeamBaseRole.Owner && myMembership.BaseRole != TeamBaseRole.Manager)
                return Forbid();

            var targetMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.Id == memberId && tm.TeamId == teamId);

            if (targetMember == null) return NotFound();
            if (targetMember.BaseRole == TeamBaseRole.Owner)
                return BadRequest("Não é possível remover o dono. Transfira a propriedade primeiro.");

            _context.TeamMembers.Remove(targetMember);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Membro removido com sucesso" });
        }

        // POST /api/teams/{teamId}/transfer-ownership - Transferir propriedade
        public class TransferOwnershipRequest { public string NewOwnerId { get; set; } = string.Empty; }

        [HttpPost("{teamId}/transfer-ownership")]
        public async Task<IActionResult> TransferOwnership(Guid teamId, [FromBody] TransferOwnershipRequest req)
        {
            var userId = CurrentUserId();
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) return NotFound();
            if (team.OwnerId != userId) return Forbid();

            var newOwnerMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == req.NewOwnerId && tm.IsActive);

            if (newOwnerMembership == null)
                return BadRequest("Novo dono precisa ser membro ativo do time");

            // Atualizar papel do novo dono
            newOwnerMembership.BaseRole = TeamBaseRole.Owner;

            // Rebaixar dono atual para Manager
            var oldOwnerMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
            if (oldOwnerMembership != null)
                oldOwnerMembership.BaseRole = TeamBaseRole.Manager;

            // Atualizar OwnerId do time
            team.OwnerId = req.NewOwnerId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Propriedade transferida com sucesso" });
        }

        // PUT /api/teams/{teamId}/members/{memberId}/role - Atualizar papel de membro
        public class UpdateMemberRoleRequest { public string Role { get; set; } = string.Empty; }

        [HttpPut("{teamId}/members/{memberId}/role")]
        public async Task<IActionResult> UpdateMemberRole(Guid teamId, Guid memberId, [FromBody] UpdateMemberRoleRequest req)
        {
            var userId = CurrentUserId();
            var myMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.IsActive);

            if (myMembership == null) return Forbid();
            if (myMembership.BaseRole != TeamBaseRole.Owner && myMembership.BaseRole != TeamBaseRole.Manager)
                return Forbid();

            var targetMember = await _context.TeamMembers.FindAsync(memberId);
            if (targetMember == null || targetMember.TeamId != teamId) return NotFound();
            if (targetMember.BaseRole == TeamBaseRole.Owner)
                return BadRequest("Use o endpoint de transferência de propriedade para mudar o dono");

            if (!Enum.TryParse<TeamBaseRole>(req.Role, out var newRole))
                return BadRequest("Papel inválido");

            if (newRole == TeamBaseRole.Owner)
                return BadRequest("Use o endpoint de transferência de propriedade");

            targetMember.BaseRole = newRole;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Papel atualizado com sucesso" });
        }
    }
}
