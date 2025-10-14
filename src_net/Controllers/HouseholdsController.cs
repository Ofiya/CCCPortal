using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HouseholdsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HouseholdsController> _logger;

        public HouseholdsController(IConfiguration configuration, ILogger<HouseholdsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetHouseholds([FromQuery] string? search = null)
        {
            try
            {
                await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        h.Id, h.Name, h.Address, h.PrimaryPhone, h.Email,
                        m.FullName as HeadName,
                        (SELECT COUNT(*) FROM Members WHERE HouseholdId = h.Id AND IsActive = 1) as MemberCount
                    FROM Households h
                    LEFT JOIN Members m ON h.HeadMemberId = m.Id
                    WHERE h.IsActive = 1
                    AND (@Search IS NULL OR h.Name LIKE '%' + @Search + '%' OR m.FullName LIKE '%' + @Search + '%' OR h.Address LIKE '%' + @Search + '%')
                    ORDER BY h.Name";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Search", string.IsNullOrEmpty(search) ? (object)DBNull.Value : search);
                
                await using var reader = await command.ExecuteReaderAsync();
                var households = new List<object>();

                while (await reader.ReadAsync())
                {
                    households.Add(new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("Id")),
                        name = reader.GetString(reader.GetOrdinal("Name")),
                        headName = reader.IsDBNull(reader.GetOrdinal("HeadName")) ? "Not Assigned" : reader.GetString(reader.GetOrdinal("HeadName")),
                        address = reader.GetString(reader.GetOrdinal("Address")),
                        primaryPhone = reader.GetString(reader.GetOrdinal("PrimaryPhone")),
                        email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        memberCount = reader.GetInt32(reader.GetOrdinal("MemberCount"))
                    });
                }

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
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is required" });

                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { error = "Household name is required" });

                if (string.IsNullOrWhiteSpace(request.Address))
                    return BadRequest(new { error = "Address is required" });

                await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO Households (Name, HeadMemberId, Address, PrimaryPhone, Email, Notes, IsActive, CreatedAt)
                    OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.Address, INSERTED.PrimaryPhone, INSERTED.Email, INSERTED.HeadMemberId
                    VALUES (@Name, @HeadMemberId, @Address, @PrimaryPhone, @Email, @Notes, 1, GETUTCDATE())";

                await using var command = new SqlCommand(query, connection);
                
                command.Parameters.AddWithValue("@Name", request.Name.Trim());
                command.Parameters.AddWithValue("@HeadMemberId", request.HeadMemberId > 0 ? (object)request.HeadMemberId : DBNull.Value);
                command.Parameters.AddWithValue("@Address", request.Address.Trim());
                command.Parameters.AddWithValue("@PrimaryPhone", request.PrimaryPhone?.Trim() ?? "");
                command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(request.Email) ? (object)DBNull.Value : request.Email.Trim());
                command.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(request.Notes) ? (object)DBNull.Value : request.Notes.Trim());

                await using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var household = new
                    {
                        id = reader.GetInt32(reader.GetOrdinal("Id")),
                        name = reader.GetString(reader.GetOrdinal("Name")),
                        address = reader.GetString(reader.GetOrdinal("Address")),
                        primaryPhone = reader.GetString(reader.GetOrdinal("PrimaryPhone")),
                        email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        headMemberId = reader.IsDBNull(reader.GetOrdinal("HeadMemberId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("HeadMemberId"))
                    };
                    
                    return StatusCode(201, household);
                }

                return BadRequest(new { error = "Failed to create household" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create household: {HouseholdName}", request?.Name);
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