using System.ComponentModel.DataAnnotations;

namespace WorldCupPredictor.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Email { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; } = "Member"; // SuperAdmin, Admin, Member

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<TournamentMember> TournamentMembers { get; set; } = new List<TournamentMember>();
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
        public ICollection<BonusAnswer> BonusAnswers { get; set; } = new List<BonusAnswer>();
    }
}
