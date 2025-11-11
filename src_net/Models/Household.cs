using System.ComponentModel.DataAnnotations;

namespace MembershipAppBEAPI.Models
{
    public class Household
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? HeadMemberId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string PrimaryPhone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Member? HeadMember { get; set; }

        public ICollection<Member> Members { get; set; } = new List<Member>();
    }
}
