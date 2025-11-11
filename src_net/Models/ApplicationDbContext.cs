using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MembershipAppBEAPI.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }


        public DbSet<MemberFollowUp> MemberFollowUps { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Attendance> Attendance { get; set; }
        public DbSet<Household> Households { get; set; }
        public DbSet<Church> Churches { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>();

            // Attendance: One member, many attendance records
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Member)
                .WithMany(m => m.AttendanceRecords)
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // Household: One household, many members
            modelBuilder.Entity<Member>()
                .HasOne(m => m.Household)
                .WithMany(h => h.Members)
                .HasForeignKey(m => m.HouseholdId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.FullName);

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.MemberId, a.ServiceDate })
                .IsUnique();



            //modelBuilder.Entity<User>().HasData(
            //    new User
            //    {
            //        Id = Guid.NewGuid().ToString(),
            //        UserName = "admin@cccredemption.org",
            //        NormalizedUserName = "ADMIN@CCCREDEMPTION.ORG",
            //        Email = "admin@cccredemption.org",
            //        NormalizedEmail = "ADMIN@CCCREDEMPTION.ORG",
            //        EmailConfirmed = true,
            //        PasswordHash = new PasswordHasher<User>()
            //            .HashPassword(null, "admin123"),
            //        RoleLevel = 3,
            //        IsActive = true,
            //        FullName = "System Administrator"
            //    }
            //);

            // User configuration - only using properties that actually exist
            modelBuilder.Entity<User>(entity =>
            {
                
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