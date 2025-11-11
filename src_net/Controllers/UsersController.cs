using MembershipAppBEAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<UsersController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "3")] // Only admins
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _dbContext.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.RoleLevel,
                        u.IsActive,
                        u.LastLogin,
                        u.CreatedAt,
                        u.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch users");
                return StatusCode(500, new { error = "Failed to fetch users" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "3")] // Only admins
        public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
        {
            // Validate request
            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest(new { error = "Full name is required" });

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { error = "Email is required" });

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                return BadRequest(new { error = "Password must be at least 6 characters long" });

            if (request.RoleLevel < 1 || request.RoleLevel > 3)
                return BadRequest(new { error = "Role level must be between 1 and 3" });

            try
            {
                var email = request.Email.Trim().ToLower();

                if (await _dbContext.Users.AnyAsync(u => u.Email == email && u.IsActive))
                    return Conflict(new { error = "User with this email already exists" });

                var passwordHash = HashPassword(request.Password);

                var user = new User
                {
                    FullName = request.FullName.Trim(),
                    Email = email,
                    PasswordHash = passwordHash,
                    RoleLevel = request.RoleLevel,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();

                return StatusCode(201, new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.RoleLevel,
                    user.CreatedAt
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Duplicate email attempt: {Email}", request.Email);
                return Conflict(new { error = "User with this email already exists" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user: {Email}", request.Email);
                return StatusCode(500, new { error = "Failed to create user" });
            }
        }

        [HttpPut("{id}/password")]
        [Authorize(Roles = "3")] // Only admins
        public async Task<IActionResult> ResetPassword(int id, [FromBody] PasswordResetRequest request)
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return BadRequest(new { error = "New password must be at least 6 characters long" });

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
                return BadRequest(new { error = "Please use the profile page to change your own password" });

            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
                if (user == null)
                    return NotFound(new { error = "User not found" });

                user.PasswordHash = HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Password reset for user ID: {UserId} by admin: {AdminId}", id, currentUserId);
                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset password for user ID: {UserId}", id);
                return StatusCode(500, new { error = "Failed to reset password" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "3")] // Only admins
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateRequest request)
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid user ID" });

            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            if (string.IsNullOrWhiteSpace(request.FullName))
                return BadRequest(new { error = "Full name is required" });

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { error = "Email is required" });

            if (request.RoleLevel < 1 || request.RoleLevel > 3)
                return BadRequest(new { error = "Role level must be between 1 and 3" });

            try
            {
                var email = request.Email.Trim().ToLower();

                if (await _dbContext.Users.AnyAsync(u => u.Email == email && u.Id != id && u.IsActive))
                    return Conflict(new { error = "User with this email already exists" });

                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
                if (user == null)
                    return NotFound(new { error = "User not found" });

                user.FullName = request.FullName.Trim();
                user.Email = email;
                user.RoleLevel = request.RoleLevel;
                user.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "User updated successfully" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Duplicate email during update: {Email}", request.Email);
                return Conflict(new { error = "User with this email already exists" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user with ID: {UserId}", id);
                return StatusCode(500, new { error = "Failed to update user" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "3")] // Only admins
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid user ID" });

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
                return BadRequest(new { error = "Cannot delete your own account" });

            try
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
                if (user == null)
                    return NotFound(new { error = "User not found" });

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("User deleted: {UserId} by admin: {AdminId}", id, currentUserId);
                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user with ID: {UserId}", id);
                return StatusCode(500, new { error = "Failed to delete user" });
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public class UserCreateRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleLevel { get; set; } = 1; // 1=User, 2=Moderator, 3=Admin
    }

    public class UserUpdateRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleLevel { get; set; } = 1;
    }

    public class PasswordResetRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}