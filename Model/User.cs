using Microsoft.AspNetCore.Identity;

namespace rezapAPI.Model
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        public string? CustomAvatarUrl { get; set; } // URL customizada ou data URI
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Relacionamento: um usuário pode ter várias tarefas
        public ICollection<Task> Tasks { get; set; } = new List<Task>();

        // Times nos quais o usuário é membro
        public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    }
}
