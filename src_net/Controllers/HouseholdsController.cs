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
    public class HouseholdsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HouseholdsController> _logger;

        public HouseholdsController(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<HouseholdsController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetHouseholds([FromQuery] string? search = null)
        {
            try
            {
                var query = _dbContext.Households
                    .Where(h => h.IsActive)
                    .Include(h => h.HeadMember)
                    .Select(h => new
                    {
                        h.Id,
                        h.Name,
                        h.Address,
                        h.PrimaryPhone,
                        h.Email,
                        HeadName = h.HeadMember != null ? h.HeadMember.FullName : "Not Assigned",
                        MemberCount = _dbContext.Members.Count(m => m.HouseholdId == h.Id && m.IsActive)
                    });

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.Trim().ToLower();
                    query = query.Where(h =>
                        h.Name.ToLower().Contains(searchTerm) ||
                        h.Address.ToLower().Contains(searchTerm) ||
                        h.HeadName.ToLower().Contains(searchTerm));
                }

                var households = await query
                    .OrderBy(h => h.Name)
                    .ToListAsync();

                return Ok(households);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch households");
                return StatusCode(500, new { error = "Failed to fetch households" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateHousehold([FromBody] HouseholdCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "Household name is required" });

            if (string.IsNullOrWhiteSpace(request.Address))
                return BadRequest(new { error = "Address is required" });

            try
            {
                var household = new Household
                {
                    Name = request.Name.Trim(),
                    HeadMemberId = request.HeadMemberId > 0 ? request.HeadMemberId : null,
                    Address = request.Address.Trim(),
                    PrimaryPhone = request.PrimaryPhone?.Trim() ?? string.Empty,
                    Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Households.Add(household);
                await _dbContext.SaveChangesAsync();

                var response = new
                {
                    household.Id,
                    household.Name,
                    household.Address,
                    household.PrimaryPhone,
                    household.Email,
                    household.HeadMemberId
                };

                return StatusCode(201, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create household: {HouseholdName}", request.Name);
                return StatusCode(500, new { error = "Failed to create household" });
            }
        }
    }
    

    public class HouseholdCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public int HeadMemberId { get; set; }
        public string Address { get; set; } = string.Empty;
        public string PrimaryPhone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Notes { get; set; }
    }
}