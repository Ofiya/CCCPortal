using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MembershipAppBEAPI.Models
{
    public class MemberFollowUp
    {
        [Key]
        public int Id { get; set; }

        // Link to the member being followed up
        [Required]
        public int MemberId { get; set; }

        [ForeignKey(nameof(MemberId))]
        public Member Member { get; set; }

        // Who performed the follow-up
        public int? FollowedById { get; set; }

        [ForeignKey(nameof(FollowedById))]
        public User? FollowedBy { get; set; }

        // Date of the follow-up
        [Required]
        public DateTime FollowUpDate { get; set; } = DateTime.UtcNow;

        // The reason or trigger for follow-up (e.g. absent for 3 weeks, welfare concern)
        [MaxLength(200)]
        public string? Reason { get; set; }

        // Notes from the person who did the follow-up
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Member’s current welfare status or feedback after follow-up
        [MaxLength(100)]
        public FollowUpOutcome? Outcome { get; set; } // e.g., “ReachedAndFine, NeedsAssistance, NotReachable, ReferredToPastor, Other”

        // Optionally, flag if further follow-up is needed
        public bool RequiresFurtherFollowUp { get; set; } = false;

        // When next follow-up should occur (if needed)
        public DateTime? NextFollowUpDate { get; set; }

        // Soft delete or archival flag
        public bool IsActive { get; set; } = true;
    }

    public enum FollowUpOutcome
    {
        ReachedAndFine,
        NeedsAssistance,
        NotReachable,
        ReferredToPastor,
        Other
    }
}
