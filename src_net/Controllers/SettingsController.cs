using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(IConfiguration configuration, ILogger<SettingsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        ChurchName, ChurchAddress, ChurchPhone, ChurchEmail,
                        CreatedAt, UpdatedAt
                    FROM Settings 
                    WHERE Id = 1"; // Assuming single settings record

                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var settings = new
                    {
                        ChurchName = reader.GetString(reader.GetOrdinal("ChurchName")),
                        ChurchAddress = reader.GetString(reader.GetOrdinal("ChurchAddress")),
                        ChurchPhone = reader.IsDBNull(reader.GetOrdinal("ChurchPhone")) ? null : reader.GetString(reader.GetOrdinal("ChurchPhone")),
                        ChurchEmail = reader.IsDBNull(reader.GetOrdinal("ChurchEmail")) ? null : reader.GetString(reader.GetOrdinal("ChurchEmail")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                    };

                    return Ok(settings);
                }

                return NotFound(new { error = "Settings not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch settings");
                return StatusCode(500, new { error = "Failed to fetch settings" });
            }
        }

        [HttpPut]
        [Authorize(Roles = "3")] // Only admins
        public async Task<IActionResult> UpdateSettings([FromBody] SettingsUpdateRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ChurchName))
            {
                return BadRequest(new { error = "Church name is required" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    UPDATE Settings 
                    SET ChurchName = @ChurchName, ChurchAddress = @ChurchAddress, 
                        ChurchPhone = @ChurchPhone, ChurchEmail = @ChurchEmail,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = 1";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ChurchName", request.ChurchName.Trim());
                command.Parameters.AddWithValue("@ChurchAddress", request.ChurchAddress?.Trim() ?? "");
                command.Parameters.AddWithValue("@ChurchPhone", request.ChurchPhone?.Trim() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ChurchEmail", request.ChurchEmail?.Trim() ?? (object)DBNull.Value);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    // Create settings if they don't exist
                    var createQuery = @"
                        INSERT INTO Settings (ChurchName, ChurchAddress, ChurchPhone, ChurchEmail, CreatedAt)
                        VALUES (@ChurchName, @ChurchAddress, @ChurchPhone, @ChurchEmail, GETUTCDATE())";
                    
                    await using var createCommand = new SqlCommand(createQuery, connection);
                    createCommand.Parameters.AddWithValue("@ChurchName", request.ChurchName.Trim());
                    createCommand.Parameters.AddWithValue("@ChurchAddress", request.ChurchAddress?.Trim() ?? "");
                    createCommand.Parameters.AddWithValue("@ChurchPhone", request.ChurchPhone?.Trim() ?? (object)DBNull.Value);
                    createCommand.Parameters.AddWithValue("@ChurchEmail", request.ChurchEmail?.Trim() ?? (object)DBNull.Value);

                    await createCommand.ExecuteNonQueryAsync();
                }

                return Ok(new { message = "Settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update settings");
                return StatusCode(500, new { error = "Failed to update settings" });
            }
        }
    }

    public class SettingsUpdateRequest
    {
        public string ChurchName { get; set; } = string.Empty;
        public string? ChurchAddress { get; set; }
        public string? ChurchPhone { get; set; }
        public string? ChurchEmail { get; set; }
    }
}