using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorldCupPredictor.Models
{
    public class BonusAnswer
    {
        [Key]
        public int Id { get; set; }

        public int QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        public BonusQuestion? Question { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required, MaxLength(200)]
        public string Answer { get; set; } = string.Empty;

        public bool? IsCorrect { get; set; }
        public int PointsEarned { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
