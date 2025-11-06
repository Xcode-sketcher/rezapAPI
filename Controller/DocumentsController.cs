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
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public DocumentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private Guid? HeaderTeamId()
        {
            if (Request.Headers.TryGetValue("X-Team-Id", out var values))
            {
                if (Guid.TryParse(values.FirstOrDefault(), out var gid))
                    return gid;
            }
            return null;
        }

        private async Task<TeamMember?> GetMembershipAsync(string userId, Guid teamId)
        {
            return await _context.TeamMembers.FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId && tm.IsActive);
        }

        private async Task<bool> CanWriteDocumentsAsync(string userId, Guid teamId)
        {
            var membership = await GetMembershipAsync(userId, teamId);
            if (membership == null) return false;
            if (membership.BaseRole == TeamBaseRole.Owner || membership.BaseRole == TeamBaseRole.Manager) return true;
            return await _context.TeamRoleGrants.AnyAsync(g => g.TeamMemberId == membership.Id && g.Scope == "documents:write" && g.GrantType == GrantType.Allow);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> List()
        {
            var userId = CurrentUserId();
            var teamId = HeaderTeamId();
            if (!teamId.HasValue) return BadRequest("X-Team-Id é obrigatório");

            var membership = await GetMembershipAsync(userId, teamId.Value);
            if (membership == null) return NotFound();

            var docs = await _context.Documents
                .Where(d => d.TeamId == teamId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new {
                    id = d.Id,
                    title = d.Title,
                    fileName = d.FileName,
                    contentType = d.ContentType,
                    size = d.Size,
                    createdAt = d.CreatedAt,
                    ownerUserId = d.OwnerUserId
                })
                .ToListAsync();
            return Ok(docs);
        }

        [HttpPost]
        [RequestSizeLimit(100_000_000)] // 100 MB
        public async Task<ActionResult<object>> Upload([FromForm] IFormFile file, [FromForm] string? title)
        {
            var userId = CurrentUserId();
            var teamId = HeaderTeamId();
            if (!teamId.HasValue) return BadRequest("X-Team-Id é obrigatório");

            if (!await CanWriteDocumentsAsync(userId, teamId.Value)) return Forbid();
            if (file == null || file.Length == 0) return BadRequest("Arquivo inválido");

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var data = ms.ToArray();

            var doc = new Document
            {
                TeamId = teamId.Value,
                OwnerUserId = userId,
                Title = string.IsNullOrWhiteSpace(title) ? System.IO.Path.GetFileNameWithoutExtension(file.FileName) : title!.Trim(),
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                Data = data,
                Size = file.Length,
                CreatedAt = DateTime.UtcNow
            };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Download), new { id = doc.Id }, new { doc.Id });
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(Guid id)
        {
            var userId = CurrentUserId();
            var teamId = HeaderTeamId();
            if (!teamId.HasValue) return BadRequest("X-Team-Id é obrigatório");

            var membership = await GetMembershipAsync(userId, teamId.Value);
            if (membership == null) return NotFound();

            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.TeamId == teamId);
            if (doc == null) return NotFound();

            return File(doc.Data, doc.ContentType, doc.FileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = CurrentUserId();
            var teamId = HeaderTeamId();
            if (!teamId.HasValue) return BadRequest("X-Team-Id é obrigatório");

            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.TeamId == teamId);
            if (doc == null) return NotFound();

            var membership = await GetMembershipAsync(userId, teamId.Value);
            if (membership == null) return NotFound();
            var isAdmin = membership.BaseRole == TeamBaseRole.Owner || membership.BaseRole == TeamBaseRole.Manager;
            if (!isAdmin && !string.Equals(doc.OwnerUserId, userId, StringComparison.Ordinal)) return Forbid();

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
