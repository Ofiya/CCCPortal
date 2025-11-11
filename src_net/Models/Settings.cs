using System.ComponentModel.DataAnnotations.Schema;

namespace MembershipAppBEAPI.Models
{
    public class Settings
    {
        public int Id { get; set; }
        public string ChurchName { get; set; } = string.Empty;
        public string ChurchAddress { get; set; } = string.Empty;
        public string? ChurchPhone { get; set; }
        public string? ChurchEmail { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
