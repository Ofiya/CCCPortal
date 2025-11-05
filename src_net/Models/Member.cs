// Updated Member.cs model
using System.ComponentModel.DataAnnotations;

namespace MembershipAppBEAPI.Models
{
    public class Member
    {
        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? MaritalStatus { get; set; }
        public int? HouseholdId { get; set; }
        public string? RankInChurch { get; set; }
        public string? ImmigrationStatus { get; set; }
        public DateTime? DocumentExpiry { get; set; }
        public DateTime? DateJoined { get; set; }
        public int? WelfareMemberId { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public bool IsFlagged { get; set; }
        public DateTime? AbsentSince { get; set; }
        public string? AdditionalNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public string? HouseholdName { get; set; }
        public string? WelfareMemberName { get; set; }
    }
}