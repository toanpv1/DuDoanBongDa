using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorldCupPredictor.Models
{
    public class Round
    {
        [Key]
        public int Id { get; set; }

        public int TournamentId { get; set; }

        [ForeignKey("TournamentId")]
        public Tournament? Tournament { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Vòng bảng", "Vòng 32", etc.

        [Required, MaxLength(20)]
        public string ShortName { get; set; } = string.Empty; // GROUP, R32, R16, QF, SF, FINAL, THIRD

        public int PointsForCorrect { get; set; } = 1;

        public int SortOrder { get; set; } = 0;

        // Navigation
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}
