using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IConfiguration configuration, ILogger<UsersController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "3")] // Only admins
        public async Task<IActionResult> GetUsers()
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT Id, FullName, Email, RoleLevel, IsActive, LastLogin, CreatedAt, UpdatedAt
                    FROM Users 
                    WHERE IsActive = 1
                    ORDER BY FullName";

                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();
                var users = new List<object>();

                while (await reader.ReadAsync())
                {
                    users.Add(new
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        RoleLevel = reader.GetInt32(reader.GetOrdinal("RoleLevel")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                        LastLogin = reader.IsDBNull(reader.GetOrdinal("LastLogin")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("LastLogin")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    });
                }

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
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new { error = "Full name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { error = "Email is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            {
                return BadRequest(new { error = "Password must be at least 6 characters long" });
            }

            if (request.RoleLevel < 1 || request.RoleLevel > 3)
            {
                return BadRequest(new { error = "Role level must be between 1 and 3" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                // Check if email already exists
                var checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND IsActive = 1";
                await using var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@Email", request.Email.Trim().ToLower());
                
                var existingCount = (int?)await checkCommand.ExecuteScalarAsync() ?? 0;
                if (existingCount > 0)
                {
                    return Conflict(new { error = "User with this email already exists" });
                }

                // Hash password
                var passwordHash = HashPassword(request.Password);

                var query = @"
                    INSERT INTO Users (FullName, Email, PasswordHash, RoleLevel, IsActive, CreatedAt)
                    OUTPUT INSERTED.Id, INSERTED.FullName, INSERTED.Email, INSERTED.RoleLevel, INSERTED.CreatedAt
                    VALUES (@FullName, @Email, @PasswordHash, @RoleLevel, 1, GETUTCDATE())";

                await using var command = new SqlCommand(query, connection);
                
                command.Parameters.AddWithValue("@FullName", request.FullName.Trim());
                command.Parameters.AddWithValue("@Email", request.Email.Trim().ToLower());
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@RoleLevel", request.RoleLevel);

                await using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var user = new
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        RoleLevel = reader.GetInt32(reader.GetOrdinal("RoleLevel")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                    };
                    
                    return StatusCode(201, user);
                }

                return BadRequest(new { error = "Failed to create user" });
            }
            catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
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
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return BadRequest(new { error = "New password must be at least 6 characters long" });
            }

            // Prevent self-password reset through this endpoint
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
            {
                return BadRequest(new { error = "Please use the profile page to change your own password" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var passwordHash = HashPassword(request.NewPassword);

                var query = @"
                    UPDATE Users 
                    SET PasswordHash = @PasswordHash, UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id AND IsActive = 1";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    return NotFound(new { error = "User not found" });
                }

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
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new { error = "Full name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { error = "Email is required" });
            }

            if (request.RoleLevel < 1 || request.RoleLevel > 3)
            {
                return BadRequest(new { error = "Role level must be between 1 and 3" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                // Check if email already exists for other users
                var checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND Id != @Id AND IsActive = 1";
                await using var checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@Email", request.Email.Trim().ToLower());
                checkCommand.Parameters.AddWithValue("@Id", id);
                
                var existingCount = (int?)await checkCommand.ExecuteScalarAsync() ?? 0;
                if (existingCount > 0)
                {
                    return Conflict(new { error = "User with this email already exists" });
                }

                var query = @"
                    UPDATE Users 
                    SET FullName = @FullName, Email = @Email, RoleLevel = @RoleLevel, 
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id AND IsActive = 1";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@FullName", request.FullName.Trim());
                command.Parameters.AddWithValue("@Email", request.Email.Trim().ToLower());
                command.Parameters.AddWithValue("@RoleLevel", request.RoleLevel);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    return NotFound(new { error = "User not found" });
                }

                return Ok(new { message = "User updated successfully" });
            }
            catch (SqlException ex) when (ex.Number == 2627)
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
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            // Prevent self-deletion
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id.ToString())
            {
                return BadRequest(new { error = "Cannot delete your own account" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                // Soft delete - set IsActive = 0
                var query = @"
                    UPDATE Users 
                    SET IsActive = 0, UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id AND IsActive = 1";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    return NotFound(new { error = "User not found" });
                }

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