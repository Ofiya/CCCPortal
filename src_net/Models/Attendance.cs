using System.ComponentModel.DataAnnotations;

namespace MembershipAppBEAPI.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MemberId { get; set; }
        public Member Member { get; set; }

        [Required]
        public DateTime ServiceDate { get; set; }

        public bool IsPresent { get; set; }
        public bool IsFlagged { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int RecordedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
