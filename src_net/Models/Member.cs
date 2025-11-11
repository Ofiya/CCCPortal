// Updated Member.cs model
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MembershipAppBEAPI.Models
{
    public class Member
    {
        [Key]
        public int Id { get; set; }

        private string _firstName = string.Empty;
        private string _lastName = string.Empty;


        public string FirstName
        {
            get => _firstName;
            set
            {
                _firstName = value;
                UpdateFullName();
            }
        }
        public string LastName
        {
            get => _lastName;
            set
            {
                _lastName = value;
                UpdateFullName();
            }
        }

        [NotMapped]
        public string FullName { get; private set; } = string.Empty;

        public void UpdateFullName() => FullName = $"{FirstName} {LastName}".Trim();
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFlagged { get; set; } = false;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? MaritalStatus { get; set; }
        
        public string? RankInChurch { get; set; }
        public string? ImmigrationStatus { get; set; }
        public DateTime? DocumentExpiry { get; set; }
        public DateTime? DateJoined { get; set; }
        public Member? WelfareMember { get; set; }
        public int? WelfareMemberId { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public DateTime? AbsentSince { get; set; }
        public string? AdditionalNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public int? HouseholdId { get; set; }
        public Household? Household { get; set; }
        public string? WelfareMemberName { get; set; }

        public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
        public ICollection<Attendance> Attendance { get; set; } = new List<Attendance>();
    }
}