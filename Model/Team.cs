using System.ComponentModel.DataAnnotations;

namespace rezapAPI.Model
{
    public enum TeamBaseRole
    {
        Owner = 0,
        Manager = 1,
        Contributor = 2
    }

    public class Team
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string OwnerId { get; set; } = string.Empty;
        public User? Owner { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
        public ICollection<TeamInvite> Invites { get; set; } = new List<TeamInvite>();
    }

    public class TeamMember
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TeamId { get; set; }
        public Team? Team { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }

        [Required]
        public TeamBaseRole BaseRole { get; set; } = TeamBaseRole.Contributor;

        public bool IsActive { get; set; } = true;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TeamRoleGrant> Grants { get; set; } = new List<TeamRoleGrant>();
    }

    public enum GrantType
    {
        Allow = 0,
        Deny = 1
    }

    public class TeamRoleGrant
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TeamMemberId { get; set; }
        public TeamMember? TeamMember { get; set; }

        [Required]
        [MaxLength(200)]
        public string Scope { get; set; } = string.Empty; // e.g. "documents:write"

        [Required]
        public GrantType GrantType { get; set; } = GrantType.Allow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum InviteStatus
    {
        Pending = 0,
        Accepted = 1,
        Revoked = 2,
        Expired = 3
    }

    public class TeamInvite
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TeamId { get; set; }
        public Team? Team { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string InvitedByUserId { get; set; } = string.Empty;
        public User? InvitedByUser { get; set; }

        public InviteStatus Status { get; set; } = InviteStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(14);
        public DateTime? RespondedAt { get; set; }
    }
}
