using Microsoft.AspNetCore.Identity;

namespace rezapAPI.Model
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Relacionamento: um usuário pode ter várias tarefas
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}
