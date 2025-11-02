using System.ComponentModel.DataAnnotations;

namespace rezapAPI.Model
{
    public class Task
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool Completed { get; set; } = false;
        
        [Required]
        public string Priority { get; set; } = "medium";
        
        public DateTime? DueDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
