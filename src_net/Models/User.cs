namespace MembershipAppBEAPI.Models
{
    public class User : Member
    {
        
        public string PasswordHash { get; set; } = string.Empty;
        public int RoleLevel { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public User User { get; set; } = new User();
    }

}