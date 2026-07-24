using Microsoft.EntityFrameworkCore;
using WorldCupPredictor.Models;

namespace WorldCupPredictor.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Tournament> Tournaments => Set<Tournament>();
        public DbSet<TournamentMember> TournamentMembers => Set<TournamentMember>();
        public DbSet<Round> Rounds => Set<Round>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<Prediction> Predictions => Set<Prediction>();
        public DbSet<BonusQuestion> BonusQuestions => Set<BonusQuestion>();
        public DbSet<BonusAnswer> BonusAnswers => Set<BonusAnswer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Convert entity table and column names to snake_case for PostgreSQL / Supabase compatibility
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entity.GetTableName();
                if (!string.IsNullOrEmpty(tableName))
                {
                    entity.SetTableName(ToSnakeCase(tableName));
                }

                foreach (var property in entity.GetProperties())
                {
                    var storeObject = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
                    var columnName = property.GetColumnName(storeObject);
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        property.SetColumnName(ToSnakeCase(columnName));
                    }
                }
            }

            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<TournamentMember>()
                .HasIndex(tm => new { tm.TournamentId, tm.UserId })
                .IsUnique();

            modelBuilder.Entity<Prediction>()
                .HasIndex(p => new { p.MatchId, p.UserId })
                .IsUnique();

            modelBuilder.Entity<BonusAnswer>()
                .HasIndex(ba => new { ba.QuestionId, ba.UserId })
                .IsUnique();

            // Seed default SuperAdmin
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                DisplayName = "Administrator",
                Role = "SuperAdmin",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return System.Text.RegularExpressions.Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
        }
    }
}
