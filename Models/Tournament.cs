using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorldCupPredictor.Models
{
    public class Tournament
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required, MaxLength(20)]
        public string PredictionType { get; set; } = "Score"; // Score, Result, Both

        [Required, MaxLength(20)]
        public string PointCalculationMethod { get; set; } = "Standard"; // Standard, SharedPool

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public int CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Round> Rounds { get; set; } = new List<Round>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
        public ICollection<TournamentMember> Members { get; set; } = new List<TournamentMember>();
        public ICollection<BonusQuestion> BonusQuestions { get; set; } = new List<BonusQuestion>();
    }
}
