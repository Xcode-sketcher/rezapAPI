using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using rezapAPI.Data;
using rezapAPI.Model;
using TaskModel = rezapAPI.Model.Task;

namespace rezapAPI.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

        private Guid? GetHeaderTeamId()
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

        private async Task<bool> CanWriteTasksAsync(string userId, Guid teamId)
        {
            var membership = await GetMembershipAsync(userId, teamId);
            if (membership == null) return false;
            // Todos os papéis podem criar/editar tarefas (Owner, Manager, Contributor)
            return true;
        }

        private async Task<bool> CanDeleteTasksAsync(string userId, Guid teamId)
        {
            var membership = await GetMembershipAsync(userId, teamId);
            if (membership == null) return false;
            // Apenas Owner e Manager podem deletar tarefas
            if (membership.BaseRole == TeamBaseRole.Owner || membership.BaseRole == TeamBaseRole.Manager) return true;
            // Verifica grants específicos
            return await _context.TeamRoleGrants.AnyAsync(g => g.TeamMemberId == membership.Id && g.Scope == "tasks:delete" && g.GrantType == GrantType.Allow);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskModel>>> GetAll()
        {
            var userId = GetCurrentUserId();
            var teamId = GetHeaderTeamId();
            var query = _context.Tasks.AsQueryable();
            query = query.Where(t => t.UserId == userId);
            if (teamId.HasValue)
                query = query.Where(t => t.TeamId == teamId);
            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskModel>> GetById(int id)
        {
            var userId = GetCurrentUserId();
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            
            if (task == null)
                return NotFound();
            
            return Ok(task);
        }

        [HttpGet("column/{columnId}")]
        public async Task<ActionResult<IEnumerable<TaskModel>>> GetByColumn(string columnId)
        {
            var userId = GetCurrentUserId();
            var teamId = GetHeaderTeamId();
            var query = _context.Tasks.AsQueryable();
            query = query.Where(t => t.ColumnId == columnId && t.UserId == userId);
            if (teamId.HasValue)
                query = query.Where(t => t.TeamId == teamId);
            var tasks = await query
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
            
            return Ok(tasks);
        }

        [HttpPost]
        public async Task<ActionResult<TaskModel>> Create([FromBody] TaskModel task)
        {
            if (string.IsNullOrWhiteSpace(task.Title))
                return BadRequest("Title is required");

            var userId = GetCurrentUserId();
            var teamId = GetHeaderTeamId();

            // Verifica permissão se estiver em um time
            if (teamId.HasValue)
            {
                if (!await CanWriteTasksAsync(userId, teamId.Value))
                    return Forbid();
            }

            task.UserId = userId;
            task.TeamId = teamId;
            task.CreatedAt = DateTime.UtcNow;
            
            if (string.IsNullOrWhiteSpace(task.ColumnId))
                task.ColumnId = "todo";

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TaskModel>> Update(int id, [FromBody] TaskModel updatedTask)
        {
            var userId = GetCurrentUserId();
            var teamId = GetHeaderTeamId();
            var query = _context.Tasks.AsQueryable();
            query = query.Where(t => t.Id == id && t.UserId == userId);
            if (teamId.HasValue)
                query = query.Where(t => t.TeamId == teamId);
            var task = await query.FirstOrDefaultAsync();
            
            if (task == null)
                return NotFound();

            // Verifica permissão se estiver em um time
            if (teamId.HasValue)
            {
                if (!await CanWriteTasksAsync(userId, teamId.Value))
                    return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(updatedTask.Title))
                task.Title = updatedTask.Title;
            
            if (updatedTask.Description != null)
                task.Description = updatedTask.Description;
            
            task.Completed = updatedTask.Completed;
            
            if (!string.IsNullOrWhiteSpace(updatedTask.Priority))
                task.Priority = updatedTask.Priority;
            
            if (!string.IsNullOrWhiteSpace(updatedTask.ColumnId))
                task.ColumnId = updatedTask.ColumnId;
            
            if (updatedTask.DueDate.HasValue)
                task.DueDate = updatedTask.DueDate;

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpPatch("{id}/move")]
        public async Task<ActionResult<TaskModel>> MoveToColumn(int id, [FromBody] MoveTaskRequest request)
        {
            var userId = GetCurrentUserId();
            var teamId = GetHeaderTeamId();
            var query = _context.Tasks.AsQueryable();
            query = query.Where(t => t.Id == id && t.UserId == userId);
            if (teamId.HasValue)
                query = query.Where(t => t.TeamId == teamId);
            var task = await query.FirstOrDefaultAsync();
            
            if (task == null)
                return NotFound();

            // Verifica permissão se estiver em um time
            if (teamId.HasValue)
            {
                if (!await CanWriteTasksAsync(userId, teamId.Value))
                    return Forbid();
            }

            task.ColumnId = request.ColumnId;
            await _context.SaveChangesAsync();
            
            return Ok(task);
        }

        [HttpPut("{id}/toggle")]
        public async Task<ActionResult<TaskModel>> Toggle(int id)
        {
            var userId = GetCurrentUserId();
            var teamId = GetHeaderTeamId();
            var query = _context.Tasks.AsQueryable();
            query = query.Where(t => t.Id == id && t.UserId == userId);
            if (teamId.HasValue)
                query = query.Where(t => t.TeamId == teamId);
            var task = await query.FirstOrDefaultAsync();
            
            if (task == null)
                return NotFound();

            // Verifica permissão se estiver em um time
            if (teamId.HasValue)
            {
                if (!await CanWriteTasksAsync(userId, teamId.Value))
                    return Forbid();
            }

            task.Completed = !task.Completed;
            await _context.SaveChangesAsync();
            
            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            
            if (task == null)
                return NotFound();

            // Verifica permissão se estiver em um time
            if (task.TeamId.HasValue)
            {
                if (!await CanDeleteTasksAsync(userId, task.TeamId.Value))
                    return Forbid();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        [HttpGet("stats")]
        public async Task<ActionResult<TaskStats>> GetStats()
        {
            var userId = GetCurrentUserId();
            var userTasks = await _context.Tasks
                .Where(t => t.UserId == userId)
                .ToListAsync();
            
            var stats = new TaskStats
            {
                Total = userTasks.Count,
                Active = userTasks.Count(t => !t.Completed),
                Completed = userTasks.Count(t => t.Completed),
                HighPriority = userTasks.Count(t => t.Priority == "high" && !t.Completed),
                Overdue = userTasks.Count(t => t.DueDate.HasValue && t.DueDate < DateTime.UtcNow && !t.Completed)
            };

            return Ok(stats);
        }
    }

    public class MoveTaskRequest
    {
        public string ColumnId { get; set; } = string.Empty;
    }

    public class TaskStats
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Completed { get; set; }
        public int HighPriority { get; set; }
        public int Overdue { get; set; }
    }
}
