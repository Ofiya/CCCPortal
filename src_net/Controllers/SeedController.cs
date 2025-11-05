using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SeedController> _logger;

        public SeedController(IConfiguration configuration, ILogger<SeedController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SeedDatabase()
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                // Create admin user if not exists
                var createUserQuery = @"
                    IF NOT EXISTS (SELECT 1 FROM Members WHERE Email = 'admin@cccredemption.org')
                    BEGIN
                        INSERT INTO Members (FullName, Email, PasswordHash, RoleLevel, IsActive, CreatedAt)
                        VALUES ('System Administrator', 'admin@cccredemption.org', 'admin123', 3, 1, GETUTCDATE())
                    END";

                await using var userCommand = new SqlCommand(createUserQuery, connection);
                await userCommand.ExecuteNonQueryAsync();

                //Create default settings if not exists
                var createSettingsQuery = @"
                    IF NOT EXISTS (SELECT 1 FROM Churches)
                    BEGIN
                        INSERT INTO Churches (ChurchName, ChurchAddress, ChurchPhone, ChurchEmail)
                        VALUES ('CCC Redemption Parish', '787 Toronto Street, Winnipeg, Manitoba, R3E 1Z7', '+1-204-555-0123', 'info@cccredemption.org')
                    END";

                await using var settingsCommand = new SqlCommand(createSettingsQuery, connection);
                await settingsCommand.ExecuteNonQueryAsync();

                return Ok(new { message = "Database seeded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed database"); // ✅ Using the exception
                return StatusCode(500, new { error = "Failed to seed database" });
            }
        }
    }
}