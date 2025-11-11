using MembershipAppBEAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<SettingsController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = await _dbContext.Settings.FirstOrDefaultAsync();

                if (settings == null)
                    return NotFound(new { error = "Settings not found" });

                var response = new
                {
                    settings.ChurchName,
                    settings.ChurchAddress,
                    settings.ChurchPhone,
                    settings.ChurchEmail,
                    settings.CreatedAt,
                    settings.UpdatedAt
                };

                return Ok(response);
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
                return BadRequest(new { error = "Request body is required" });

            if (string.IsNullOrWhiteSpace(request.ChurchName))
                return BadRequest(new { error = "Church name is required" });

            try
            {
                var settings = await _dbContext.Settings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    // Create new settings
                    settings = new Settings
                    {
                        ChurchName = request.ChurchName.Trim(),
                        ChurchAddress = request.ChurchAddress?.Trim() ?? string.Empty,
                        ChurchPhone = string.IsNullOrWhiteSpace(request.ChurchPhone) ? null : request.ChurchPhone.Trim(),
                        ChurchEmail = string.IsNullOrWhiteSpace(request.ChurchEmail) ? null : request.ChurchEmail.Trim(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.Settings.Add(settings);
                }
                else
                {
                    // Update existing settings
                    settings.ChurchName = request.ChurchName.Trim();
                    settings.ChurchAddress = request.ChurchAddress?.Trim() ?? string.Empty;
                    settings.ChurchPhone = string.IsNullOrWhiteSpace(request.ChurchPhone) ? null : request.ChurchPhone.Trim();
                    settings.ChurchEmail = string.IsNullOrWhiteSpace(request.ChurchEmail) ? null : request.ChurchEmail.Trim();
                    settings.UpdatedAt = DateTime.UtcNow;

                    _dbContext.Settings.Update(settings);
                }

                await _dbContext.SaveChangesAsync();
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