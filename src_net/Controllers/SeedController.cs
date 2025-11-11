using Azure.Core;
using MembershipAppBEAPI.Models;
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
    public class SeedController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SeedController> _logger;

        public SeedController(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<SeedController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SeedDatabase()
        {
            try
            {
                // Seed Admin User
                string adminEmail = "admin@cccredemption.org";
                string adminPassword = "Admin123";
                string hashedPassword = HashPassword(adminPassword);

                var existingAdmin = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
                if (existingAdmin == null)
                {
                    var adminUser = new User
                    {
                        FullName = "System Administrator",
                        Email = adminEmail,
                        PasswordHash = hashedPassword,
                        RoleLevel = 3,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.Users.Add(adminUser);
                }

                // Seed Default Church
                var existingChurch = await _dbContext.Churches.FirstOrDefaultAsync();
                if (existingChurch == null)
                {
                    var church = new Church
                    {
                        ChurchName = "CCC Redemption Parish",
                        ChurchAddress = "787 Toronto Street, Winnipeg, Manitoba, R3E 1Z7",
                        ChurchPhone = "+1-204-555-0123",
                        ChurchEmail = "info@cccredemption.org"
                    };
                    _dbContext.Churches.Add(church);
                }

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Database seeded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed database");
                return StatusCode(500, new { error = "Failed to seed database" });
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
}