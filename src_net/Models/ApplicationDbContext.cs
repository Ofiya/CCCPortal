using Microsoft.EntityFrameworkCore;

namespace MembershipAppBEAPI.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration - only using properties that actually exist
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(255);
                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);
                entity.Property(e => e.PasswordHash)
                    .IsRequired();
                entity.Property(e => e.RoleLevel)
                    .HasDefaultValue(1);
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}