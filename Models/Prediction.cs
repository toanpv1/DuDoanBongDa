using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorldCupPredictor.Models
{
    public class Prediction
    {
        [Key]
        public int Id { get; set; }

        public int MatchId { get; set; }

        [ForeignKey("MatchId")]
        public Match? Match { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int? PredictedHomeScore { get; set; }
        public int? PredictedAwayScore { get; set; }

        [MaxLength(10)]
        public string? PredictedResult { get; set; } // Home, Draw, Away

        public double PointsEarned { get; set; } = 0;
        public bool? IsCorrect { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Computed prediction result from scores
        [NotMapped]
        public string? ComputedResult
        {
            get
            {
                if (PredictedHomeScore == null || PredictedAwayScore == null)
                    return PredictedResult;
                if (PredictedHomeScore > PredictedAwayScore) return "Home";
                if (PredictedHomeScore < PredictedAwayScore) return "Away";
                return "Draw";
            }
        }
    }
}
