using System.ComponentModel.DataAnnotations;

namespace rezapAPI.Model
{
    public class Task
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Completed { get; set; }
        public string Priority { get; set; } = "medium"; // low, medium, high
        public string? ColumnId { get; set; } // Para o Kanban
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign key para o usuário
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
    }
}
