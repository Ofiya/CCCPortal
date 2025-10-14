using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { error = "Email and password are required" });
                }

                await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                var query = @"
                    SELECT Id, FullName, Email, PasswordHash, RoleLevel, IsActive
                    FROM Users 
                    WHERE Email = @Email AND IsActive = 1";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", request.Email.Trim().ToLower());

                await using var reader = await command.ExecuteReaderAsync();
                
                if (!await reader.ReadAsync())
                {
                    _logger.LogWarning("Login failed: User not found - {Email}", request.Email);
                    return Unauthorized(new { error = "Invalid email or password" });
                }

                // Read user data
                var userId = reader.GetInt32(reader.GetOrdinal("Id"));
                var fullName = reader.GetString(reader.GetOrdinal("FullName"));
                var email = reader.GetString(reader.GetOrdinal("Email"));
                var storedHash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                var roleLevel = reader.GetInt32(reader.GetOrdinal("RoleLevel"));
                var isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));

                await reader.CloseAsync();

                // Verify password
                var inputHash = HashPassword(request.Password);
                if (inputHash != storedHash)
                {
                    _logger.LogWarning("Login failed: Invalid password for user {UserId}", userId);
                    return Unauthorized(new { error = "Invalid email or password" });
                }

                // Update last login
                var updateQuery = "UPDATE Users SET LastLogin = GETUTCDATE() WHERE Id = @Id";
                await using var updateCommand = new SqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@Id", userId);
                await updateCommand.ExecuteNonQueryAsync();

                // Generate JWT token
                var token = GenerateJwtToken(userId, fullName, email, roleLevel);

                var response = new
                {
                    token = token,
                    user = new
                    {
                        id = userId,
                        name = fullName,
                        email = email,
                        roleLevel = roleLevel
                    }
                };

                _logger.LogInformation("User {UserId} logged in successfully", userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}", request?.Email);
                return StatusCode(500, new { error = "An error occurred during login" });
            }
        }

        private string GenerateJwtToken(int userId, string fullName, string email, int roleLevel)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] 
                ?? "fallback-secret-key-minimum-32-characters-long-for-production");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, fullName),
                    new Claim(ClaimTypes.Role, roleLevel.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}