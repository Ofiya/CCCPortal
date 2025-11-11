using MembershipAppBEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _dbContext = dbContext;
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

                // Normalize email input
                var normalizedEmail = request.Email.Trim().ToLower();

                //query DB
                var user = await _dbContext.Users
                                .AsNoTracking()
                                .Where(u => u.Email.ToLower() == normalizedEmail && u.IsActive)
                                .Select(u => new
                                {
                                    u.Id,
                                    u.FullName,
                                    u.Email,
                                    u.PasswordHash,
                                    u.RoleLevel,
                                    u.IsActive
                                })
                                .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found - {Email}", request.Email);
                    return Unauthorized(new { error = "Invalid email address" });
                }

                //Verify password
                var inputHash = HashPassword(request.Password);
                if (inputHash != user.PasswordHash)
                {
                    _logger.LogWarning("Login failed: Invalid password for user {UserId}", user.Id);
                    return Unauthorized(new { error = "Invalid password" });
                }

                // Update last login
                var userEntity = await _dbContext.Users.FindAsync(user.Id);
                if (userEntity != null)
                {
                    userEntity.LastLogin = DateTime.UtcNow;
                    _dbContext.Users.Update(userEntity);
                    await _dbContext.SaveChangesAsync();
                }


                // Generate JWT token
                var token = GenerateJwtToken(user.Id, user.FullName, user.Email, user.RoleLevel);

                var response = new
                {
                    token,
                    user = new
                    {
                        id = user.Id,
                        name = user.FullName,
                        email = user.Email,
                        roleLevel = user.RoleLevel
                    }
                };
                

                _logger.LogInformation("User {UserId} logged in successfully", user.Id);
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