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
        }
    }
}
