using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using rezapAPI.Model;
using TaskModel = rezapAPI.Model.Task;

namespace rezapAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskModel> Tasks { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<TeamInvite> TeamInvites { get; set; }
        public DbSet<TeamRoleGrant> TeamRoleGrants { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<TeamAuditLog> TeamAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuração de relacionamento User -> Tasks
            builder.Entity<TaskModel>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices para performance
            builder.Entity<TaskModel>()
                .HasIndex(t => t.UserId);

            builder.Entity<TaskModel>()
                .HasIndex(t => t.ColumnId);

            builder.Entity<TaskModel>()
                .HasIndex(t => t.TeamId);

            // Team
            builder.Entity<Team>()
                .HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Team>()
                .HasIndex(t => new { t.OwnerId, t.Name })
                .IsUnique();

            // TeamMember
            builder.Entity<TeamMember>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeamMember>()
                .HasOne(tm => tm.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeamMember>()
                .HasIndex(tm => new { tm.TeamId, tm.UserId })
                .IsUnique();

            // TeamInvite
            builder.Entity<TeamInvite>()
                .HasOne(i => i.Team)
                .WithMany(t => t.Invites)
                .HasForeignKey(i => i.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeamInvite>()
                .HasOne<User>(i => i.InvitedByUser)
                .WithMany()
                .HasForeignKey(i => i.InvitedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeamInvite>()
                .HasIndex(i => new { i.TeamId, i.Email })
                .IsUnique();

            // TeamRoleGrant
            builder.Entity<TeamRoleGrant>()
                .HasOne(gr => gr.TeamMember)
                .WithMany(tm => tm.Grants)
                .HasForeignKey(gr => gr.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // Document
            builder.Entity<Document>()
                .HasIndex(d => new { d.TeamId, d.CreatedAt });
            builder.Entity<Document>()
                .Property(d => d.Size)
                .HasConversion<long>();

            // TeamAuditLog
            builder.Entity<TeamAuditLog>()
                .HasOne(a => a.Team)
                .WithMany()
                .HasForeignKey(a => a.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeamAuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeamAuditLog>()
                .HasIndex(a => new { a.TeamId, a.CreatedAt });
        }
    }
}
