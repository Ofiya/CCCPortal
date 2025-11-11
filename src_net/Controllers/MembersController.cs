using CsvHelper;
using CsvHelper.Configuration;
using MembershipAppBEAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Security.Claims;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MembersController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MembersController> _logger;

        public MembersController(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<MembersController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMembers([FromQuery] string? search = null, [FromQuery] string? gender = null, [FromQuery] string? household = null, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            // Validate input
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;
            var skip = (page - 1) * limit;

            try
            {
                var query = _dbContext.Members
                    .Include(m => m.Household)
                    .Include(m => m.WelfareMember)
                    .Where(m => m.IsActive);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(m =>
                        m.FullName.Contains(search) ||
                        m.FirstName.Contains(search) ||
                        m.LastName.Contains(search) ||
                        m.Email.Contains(search) ||
                        m.PhoneNumber.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(gender))
                {
                    query = query.Where(m => m.Gender == gender);
                }

                if (!string.IsNullOrWhiteSpace(household))
                {
                    query = query.Where(m => m.Household != null && m.Household.Name == household);
                }

                var totalCount = await query.CountAsync();

                var members = await query
                    .OrderBy(m => m.FullName)
                    .Skip(skip)
                    .Take(limit)
                    .Select(m => new MemberResponse
                    {
                        Id = m.Id,
                        FirstName = m.FirstName,
                        LastName = m.LastName,
                        FullName = m.FullName,
                        Gender = m.Gender,
                        DateOfBirth = m.DateOfBirth,
                        PhoneNumber = m.PhoneNumber,
                        Email = m.Email,
                        HouseholdName = m.Household != null ? m.Household.Name : null,
                        WelfareMemberName = m.WelfareMember != null ? m.WelfareMember.FullName : null,
                        IsFlagged = m.IsFlagged,
                        AbsentSince = m.AbsentSince,
                        ImmigrationStatus = m.ImmigrationStatus,
                        DocumentExpiry = m.DocumentExpiry,
                        DateJoined = m.DateJoined
                    })
                    .ToListAsync();

                return Ok(new
                {
                    members,
                    totalCount,
                    currentPage = page,
                    totalPages = (int)Math.Ceiling(totalCount / (double)limit)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch members");
                return StatusCode(500, new { error = "Failed to fetch members" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateMember([FromBody] MemberCreateRequest request)
        {
            if (request == null) return BadRequest(new { error = "Request body is required" });
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest(new { error = "First name and last name are required" });

            try
            {
                var fullName = string.IsNullOrWhiteSpace(request.FullName)
                    ? $"{request.FirstName.Trim()} {request.LastName.Trim()}"
                    : request.FullName.Trim();

                DateTime? dateOfBirth = null;
                if (request.DateOfBirth != null)
                {
                    if (request.DateOfBirth is DateTime dob)
                        dateOfBirth = dob;
                    else if (DateTime.TryParse(request.DateOfBirth.ToString(), out var parsedDob))
                        dateOfBirth = parsedDob;
                }

                var member = new Member
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Gender = request.Gender?.Trim() ?? "Unknown",
                    DateOfBirth = dateOfBirth,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email?.ToLower(),
                    Address = request.Address,
                    Occupation = request.Occupation,
                    MaritalStatus = request.MaritalStatus,
                    HouseholdId = request.HouseholdId,
                    RankInChurch = request.RankInChurch,
                    ImmigrationStatus = request.ImmigrationStatus,
                    DocumentExpiry = request.DocumentExpiry,
                    DateJoined = request.DateJoined ?? DateTime.UtcNow,
                    WelfareMemberId = request.WelfareMemberId,
                    AdditionalNotes = request.AdditionalNotes,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Members.Add(member);
                await _dbContext.SaveChangesAsync();

                return StatusCode(201, new
                {
                    member.Id,
                    member.FirstName,
                    member.LastName,
                    member.FullName,
                    member.Gender,
                    member.Email,
                    member.PhoneNumber,
                    member.DateOfBirth,
                    member.ImmigrationStatus,
                    member.DateJoined,
                    member.HouseholdId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create member: {FirstName} {LastName}", request.FirstName, request.LastName);
                return StatusCode(500, new { error = "Failed to create member" });
            }
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadMembersCSV(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Only CSV files are allowed" });

            if (file.Length > 25 * 1024 * 1024)
                return BadRequest(new { error = "File size exceeds 25MB limit" });

            try
            {
                using var stream = new StreamReader(file.OpenReadStream());
                var csvContent = await stream.ReadToEndAsync();
                var lines = csvContent.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

                if (lines.Length <= 1)
                    return BadRequest(new { error = "CSV file is empty or has no data rows" });

                var headers = lines[0].Split(',').Select(h => h.Trim().ToLower()).ToArray();
                var requiredColumns = new[] { "firstname", "lastname", "gender" };
                var missingColumns = requiredColumns.Where(rc => !headers.Contains(rc)).ToArray();

                if (missingColumns.Any())
                    return BadRequest(new { error = $"Missing required columns: {string.Join(", ", missingColumns)}" });

                var successCount = 0;
                var errorCount = 0;
                var errors = new List<string>();

                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i]);
                        if (values.Length < requiredColumns.Length)
                        {
                            errorCount++;
                            errors.Add($"Row {i + 1}: Insufficient columns");
                            continue;
                        }

                        var firstName = GetValueByHeader(headers, values, "firstname");
                        var lastName = GetValueByHeader(headers, values, "lastname");
                        var gender = GetValueByHeader(headers, values, "gender");
                        var phoneNumber = GetValueByHeader(headers, values, "phonenumber");
                        var email = GetValueByHeader(headers, values, "email");
                        var dateOfBirthStr = GetValueByHeader(headers, values, "dateofbirth");
                        var address = GetValueByHeader(headers, values, "address");

                        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                        {
                            errorCount++;
                            errors.Add($"Row {i + 1}: First name and last name are required");
                            continue;
                        }

                        DateTime? dateOfBirth = null;
                        if (!string.IsNullOrWhiteSpace(dateOfBirthStr))
                        {
                            if (!DateTime.TryParse(dateOfBirthStr, out var parsedDob))
                            {
                                errorCount++;
                                errors.Add($"Row {i + 1}: Invalid date format for Date of Birth");
                                continue;
                            }
                            dateOfBirth = parsedDob;
                        }

                        var fullName = $"{firstName.Trim()} {lastName.Trim()}";

                        // Check if a member with the same email or phone exists (optional)
                        var exists = await _dbContext.Members.AnyAsync(m =>
                            m.IsActive &&
                            (m.Email == email || m.PhoneNumber == phoneNumber));

                        if (exists)
                        {
                            errorCount++;
                            errors.Add($"Row {i + 1}: Member with same email or phone already exists");
                            continue;
                        }

                        // Add member using EF
                        var member = new Member
                        {
                            FirstName = firstName.Trim(),
                            LastName = lastName.Trim(),
                            Gender = gender.Trim(),
                            DateOfBirth = dateOfBirth,
                            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
                            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLower(),
                            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _dbContext.Members.Add(member);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"Row {i + 1}: {ex.Message}");
                    }
                }

                // Save all successfully added members at once
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    message = $"CSV import completed. Success: {successCount}, Errors: {errorCount}",
                    successCount,
                    errorCount,
                    errors = errors.Take(10).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process CSV upload");
                return StatusCode(500, new { error = "Failed to process CSV file", details = ex.Message });
            }
        }

        // Helper method to parse CSV line considering quoted fields
        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            var inQuotes = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            result.Add(current.Trim());
            return result.ToArray();
        }

        // Helper method to get value by header name
        private string GetValueByHeader(string[] headers, string[] values, string headerName)
        {
            var index = Array.IndexOf(headers, headerName);
            return index >= 0 && index < values.Length ? values[index] : "";
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMember(int id)
        {
            if (id <= 0) return BadRequest(new { error = "Invalid member ID" });

            try
            {
                var member = await _dbContext.Members
                    .Include(m => m.Household)
                    .Include(m => m.WelfareMember)
                    .Where(m => m.Id == id && m.IsActive)
                    .Select(m => new MemberDetailResponse
                    {
                        Id = m.Id,
                        FirstName = m.FirstName,
                        LastName = m.LastName,
                        FullName = m.FullName,
                        Gender = m.Gender,
                        DateOfBirth = m.DateOfBirth,
                        PhoneNumber = m.PhoneNumber,
                        Email = m.Email,
                        Address = m.Address,
                        Occupation = m.Occupation,
                        MaritalStatus = m.MaritalStatus,
                        HouseholdId = m.HouseholdId,
                        HouseholdName = m.Household != null ? m.Household.Name : null,
                        RankInChurch = m.RankInChurch,
                        ImmigrationStatus = m.ImmigrationStatus,
                        DocumentExpiry = m.DocumentExpiry,
                        DateJoined = m.DateJoined,
                        WelfareMemberId = m.WelfareMemberId,
                        WelfareMemberName = m.WelfareMember != null ? m.WelfareMember.FullName : null,
                        IsFlagged = m.IsFlagged,
                        AbsentSince = m.AbsentSince,
                        AdditionalNotes = m.AdditionalNotes,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (member == null) return NotFound(new { error = "Member not found" });

                return Ok(member);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch member with ID: {MemberId}", id);
                return StatusCode(500, new { error = "Failed to fetch member" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMember(int id, [FromBody] MemberUpdateRequest request)
        {
            if (id <= 0) return BadRequest(new { error = "Invalid member ID" });
            if (request == null) return BadRequest(new { error = "Request body is required" });
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                return BadRequest(new { error = "First name and last name are required" });

            try
            {
                var member = await _dbContext.Members.FindAsync(id);
                if (member == null || !member.IsActive) return NotFound(new { error = "Member not found" });

                member.FirstName = request.FirstName.Trim();
                member.LastName = request.LastName.Trim();
                member.Gender = request.Gender?.Trim() ?? "Unknown";
                member.DateOfBirth = request.DateOfBirth;
                member.PhoneNumber = request.PhoneNumber;
                member.Email = request.Email;
                member.Address = request.Address;
                member.Occupation = request.Occupation;
                member.MaritalStatus = request.MaritalStatus;
                member.HouseholdId = request.HouseholdId;
                member.RankInChurch = request.RankInChurch;
                member.ImmigrationStatus = request.ImmigrationStatus;
                member.DocumentExpiry = request.DocumentExpiry;
                member.DateJoined = request.DateJoined;
                member.WelfareMemberId = request.WelfareMemberId;
                member.AdditionalNotes = request.AdditionalNotes;
                member.IsFlagged = request.IsFlagged;
                member.AbsentSince = request.AbsentSince;
                member.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Member updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update member with ID: {MemberId}", id);
                return StatusCode(500, new { error = "Failed to update member" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMember(int id)
        {
            if (id <= 0) return BadRequest(new { error = "Invalid member ID" });

            try
            {
                var member = await _dbContext.Members.FindAsync(id);
                if (member == null || !member.IsActive) return NotFound(new { error = "Member not found" });

                member.IsActive = false;
                member.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Member deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete member with ID: {MemberId}", id);
                return StatusCode(500, new { error = "Failed to delete member" });
            }
        }
    }

    // Model classes
    public class MemberResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? HouseholdName { get; set; }
        public string? WelfareMemberName { get; set; }
        public bool IsFlagged { get; set; }
        public DateTime? AbsentSince { get; set; }
        public string? ImmigrationStatus { get; set; }
        public DateTime? DocumentExpiry { get; set; }
        public DateTime? DateJoined { get; set; }
    }

    public class MemberDetailResponse : MemberResponse
    {
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? MaritalStatus { get; set; }
        public int? HouseholdId { get; set; }
        public string? RankInChurch { get; set; }
        public int? WelfareMemberId { get; set; }
        public string? AdditionalNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

        public class MemberCreateRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public object? DateOfBirth { get; set; } // Flexible date handling
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? MaritalStatus { get; set; }
        public int? HouseholdId { get; set; }
        public string? RankInChurch { get; set; }
        public string? ImmigrationStatus { get; set; }
        public DateTime? DocumentExpiry { get; set; }
        public DateTime? DateJoined { get; set; }
        public int? WelfareMemberId { get; set; }
        public string? AdditionalNotes { get; set; }
    }

    public class MemberUpdateRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? MaritalStatus { get; set; }
        public int? HouseholdId { get; set; }
        public string? RankInChurch { get; set; }
        public string? ImmigrationStatus { get; set; }
        public DateTime? DocumentExpiry { get; set; }
        public DateTime? DateJoined { get; set; }
        public int? WelfareMemberId { get; set; }
        public string? AdditionalNotes { get; set; }
        public bool IsFlagged { get; set; }
        public DateTime? AbsentSince { get; set; }
    }
}
