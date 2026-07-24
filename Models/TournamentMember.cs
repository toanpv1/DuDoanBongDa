using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorldCupPredictor.Models
{
    public class TournamentMember
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }

        [ForeignKey("TournamentId")]
        public Tournament? Tournament { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(20)]
        public string MissedMatchPolicy { get; set; } = "AllWrong"; // AllWrong, Percentage

        public double MissedMatchPercentage { get; set; } = 0; // 0-100
    }
}
