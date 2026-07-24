using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorldCupPredictor.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }

        [ForeignKey("TournamentId")]
        public Tournament? Tournament { get; set; }

        public int RoundId { get; set; }

        [ForeignKey("RoundId")]
        public Round? Round { get; set; }

        [MaxLength(10)]
        public string? GroupName { get; set; } // A, B, C... (for group stage)

        [Required, MaxLength(100)]
        public string HomeTeam { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string AwayTeam { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? HomeFlag { get; set; } // Emoji flag

        [MaxLength(10)]
        public string? AwayFlag { get; set; }

        public DateTime MatchDate { get; set; }

        public DateTime PredictionDeadline { get; set; } // Default: MatchDate - 30 min

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Scheduled"; // Scheduled, Live, Completed, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();

        // Computed
        [NotMapped]
        public string? ActualResult
        {
            get
            {
                if (HomeScore == null || AwayScore == null) return null;
                if (HomeScore > AwayScore) return "Home";
                if (HomeScore < AwayScore) return "Away";
                return "Draw";
            }
        }

        // PredictionDeadline stored as Vietnam local time (UTC+7), so compare with VN local time
        [NotMapped]
        public bool IsPredictionOpen => DateTime.UtcNow.AddHours(7) < PredictionDeadline && Status == "Scheduled";
    }
}
