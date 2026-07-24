using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorldCupPredictor.Models
{
    public class BonusQuestion
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }

        [ForeignKey("TournamentId")]
        public Tournament? Tournament { get; set; }

        [Required, MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? CorrectAnswer { get; set; }

        public int BonusPoints { get; set; } = 5;

        public DateTime Deadline { get; set; }

        public bool IsResolved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<BonusAnswer> Answers { get; set; } = new List<BonusAnswer>();
    }
}
