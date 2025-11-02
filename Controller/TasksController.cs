using Microsoft.AspNetCore.Mvc;
using rezapAPI.Model;

namespace rezapAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private static List<Model.Task> tasks = new List<Model.Task>
        {
            new Model.Task
            {
                Id = 1,
                Title = "Revisar código do projeto",
                Description = "Verificar as mudanças recentes e fazer code review",
                Completed = false,
                Priority = "high",
                DueDate = new DateTime(2025, 11, 20),
                CreatedAt = DateTime.UtcNow
            },
            new Model.Task
            {
                Id = 2,
                Title = "Atualizar documentação",
                Description = "Documentar as novas features",
                Completed = false,
                Priority = "medium",
                DueDate = new DateTime(2025, 11, 25),
                CreatedAt = DateTime.UtcNow
            },
            new Model.Task
            {
                Id = 3,
                Title = "Reunião com a equipe",
                Description = "Alinhamento do sprint",
                Completed = true,
                Priority = "low",
                DueDate = new DateTime(2025, 11, 15),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Model.Task
            {
                Id = 4,
                Title = "Implementar nova feature",
                Description = "Adicionar sistema de notificações",
                Completed = false,
                Priority = "high",
                DueDate = new DateTime(2025, 11, 30),
                CreatedAt = DateTime.UtcNow
            },
            new Model.Task
            {
                Id = 5,
                Title = "Code review semanal",
                Description = "Revisar PRs da semana",
                Completed = false,
                Priority = "medium",
                DueDate = new DateTime(2025, 11, 22),
                CreatedAt = DateTime.UtcNow
            }
        };

        [HttpGet]
        public IActionResult GetAllTasks()
        {
            return Ok(tasks);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetTaskById(int id)
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return NotFound();
            return Ok(task);
        }

        [HttpPost]
        public IActionResult CreateTask([FromBody] Model.Task newTask)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            newTask.Id = tasks.Any() ? tasks.Max(t => t.Id) + 1 : 1;
            newTask.CreatedAt = DateTime.UtcNow;
            tasks.Add(newTask);
            
            return CreatedAtAction(nameof(GetTaskById), new { id = newTask.Id }, newTask);
        }

        [HttpPut("{id:int}")]
        public IActionResult UpdateTask(int id, [FromBody] Model.Task updatedTask)
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return NotFound();

            task.Title = updatedTask.Title ?? task.Title;
            task.Description = updatedTask.Description ?? task.Description;
            task.Priority = updatedTask.Priority ?? task.Priority;
            task.DueDate = updatedTask.DueDate ?? task.DueDate;
            
           
            if (updatedTask.Completed != task.Completed)
                task.Completed = updatedTask.Completed;

            return Ok(task);
        }

        [HttpPut("{id:int}/toggle")]
        public IActionResult ToggleTask(int id)
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return NotFound();

            task.Completed = !task.Completed;
            return Ok(task);
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteTask(int id)
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) return NotFound();

            tasks.Remove(task);
            return NoContent();
        }
    }
}
