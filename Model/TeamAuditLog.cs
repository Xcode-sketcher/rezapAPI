using System.ComponentModel.DataAnnotations;

namespace rezapAPI.Model
{
    public enum AuditAction
    {
        TeamCreated = 0,
        TeamUpdated = 1,
        MemberAdded = 2,
        MemberRemoved = 3,
        MemberRoleChanged = 4,
        InviteSent = 5,
        InviteAccepted = 6,
        InviteRevoked = 7,
        TaskCreated = 8,
        TaskUpdated = 9,
        TaskDeleted = 10,
        DocumentUploaded = 11,
        DocumentDeleted = 12,
        OwnershipTransferred = 13
    }

    public class TeamAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TeamId { get; set; }
        public Team? Team { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }

        [Required]
        public AuditAction Action { get; set; }

        [MaxLength(500)]
        public string? EntityId { get; set; } // ID da entidade afetada (task, document, member, etc.)

        [MaxLength(1000)]
        public string? Details { get; set; } // JSON ou texto descritivo adicional

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
