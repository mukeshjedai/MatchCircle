using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using testapp1.Models;

namespace testapp1.Data
{
    public class MatrimonialDbContext : DbContext
    {
        public MatrimonialDbContext(DbContextOptions<MatrimonialDbContext> options)
            : base(options)
        {
        }

        public override int SaveChanges()
        {
            EnsureUtcForDateTimes();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            EnsureUtcForDateTimes();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void EnsureUtcForDateTimes()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime) && property.CurrentValue != null)
                    {
                        var dateTime = (DateTime)property.CurrentValue;
                        if (dateTime.Kind == DateTimeKind.Unspecified)
                        {
                            property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                        }
                        else if (dateTime.Kind == DateTimeKind.Local)
                        {
                            property.CurrentValue = dateTime.ToUniversalTime();
                        }
                    }
                    else if (property.Metadata.ClrType == typeof(DateTime?))
                    {
                        var dateTime = (DateTime?)property.CurrentValue;
                        if (dateTime.HasValue)
                        {
                            if (dateTime.Value.Kind == DateTimeKind.Unspecified)
                            {
                                property.CurrentValue = DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
                            }
                            else if (dateTime.Value.Kind == DateTimeKind.Local)
                            {
                                property.CurrentValue = dateTime.Value.ToUniversalTime();
                            }
                        }
                    }
                }
            }
        }

        // Users & Auth
        public DbSet<User> Users { get; set; }
        public DbSet<UserLoginSession> UserLoginSessions { get; set; }

        // Profile Details
        public DbSet<UserProfile> UserProfiles { get; set; }

        // Photos
        public DbSet<ProfilePhoto> ProfilePhotos { get; set; }

        // Verification Documents
        public DbSet<UserVerification> UserVerifications { get; set; }

        // Partner Preferences
        public DbSet<PartnerPreference> PartnerPreferences { get; set; }

        // User Interactions
        public DbSet<UserInteraction> UserInteractions { get; set; }

        // Matches
        public DbSet<Match> Matches { get; set; }

        // Messages
        public DbSet<Message> Messages { get; set; }

        // Subscriptions & Payments
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<Payment> Payments { get; set; }

        // Reporting & Safety
        public DbSet<UserReport> UserReports { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure schema
            modelBuilder.HasDefaultSchema("best");

            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<UserInteraction>()
                .HasIndex(ui => new { ui.FromUserId, ui.ToUserId, ui.InteractionType })
                .IsUnique();

            modelBuilder.Entity<Match>()
                .HasIndex(m => new { m.User1Id, m.User2Id })
                .IsUnique();

            // Configure indexes
            modelBuilder.Entity<UserProfile>()
                .HasIndex(p => new { p.Gender, p.Country, p.State, p.City })
                .HasDatabaseName("idx_profiles_gender_location");

            modelBuilder.Entity<UserProfile>()
                .HasIndex(p => new { p.Religion, p.Caste })
                .HasDatabaseName("idx_profiles_religion_caste");

            modelBuilder.Entity<UserProfile>()
                .HasIndex(p => p.DateOfBirth)
                .HasDatabaseName("idx_profiles_dob");

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.MatchId, m.CreatedAt })
                .HasDatabaseName("idx_messages_match_created");

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.ToUserId, m.IsRead })
                .HasDatabaseName("idx_messages_to_user_unread");

            // Configure UserVerification relationships
            modelBuilder.Entity<UserVerification>()
                .HasOne(uv => uv.User)
                .WithMany(u => u.Verifications)
                .HasForeignKey(uv => uv.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserVerification>()
                .HasOne(uv => uv.Reviewer)
                .WithMany()
                .HasForeignKey(uv => uv.ReviewedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure UserReport relationships
            modelBuilder.Entity<UserReport>()
                .HasOne(ur => ur.Reporter)
                .WithMany()
                .HasForeignKey(ur => ur.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserReport>()
                .HasOne(ur => ur.ReportedUser)
                .WithMany()
                .HasForeignKey(ur => ur.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserReport>()
                .HasOne(ur => ur.Handler)
                .WithMany()
                .HasForeignKey(ur => ur.HandledBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure UserInteraction relationships
            modelBuilder.Entity<UserInteraction>()
                .HasOne(ui => ui.FromUser)
                .WithMany()
                .HasForeignKey(ui => ui.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserInteraction>()
                .HasOne(ui => ui.ToUser)
                .WithMany()
                .HasForeignKey(ui => ui.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Match relationships
            modelBuilder.Entity<Match>()
                .HasOne(m => m.User1)
                .WithMany()
                .HasForeignKey(m => m.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.User2)
                .WithMany()
                .HasForeignKey(m => m.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Message relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.FromUser)
                .WithMany()
                .HasForeignKey(m => m.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.ToUser)
                .WithMany()
                .HasForeignKey(m => m.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

