using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.Security.Claims;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MembersController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MembersController> _logger;

        public MembersController(IConfiguration configuration, ILogger<MembersController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMembers(
            [FromQuery] string? search = null,
            [FromQuery] string? gender = null,
            [FromQuery] string? household = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            // Validate input
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var offset = (page - 1) * limit;

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                // Use parameterized queries to prevent SQL injection
                var whereClauses = new List<string>();
                var parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(search))
                {
                    whereClauses.Add("(m.FullName LIKE '%' + @Search + '%' OR m.Email LIKE '%' + @Search + '%' OR m.PhoneNumber LIKE '%' + @Search + '%' OR m.FirstName LIKE '%' + @Search + '%' OR m.LastName LIKE '%' + @Search + '%')");
                    parameters.Add(new SqlParameter("@Search", search));
                }

                if (!string.IsNullOrEmpty(gender))
                {
                    whereClauses.Add("m.Gender = @Gender");
                    parameters.Add(new SqlParameter("@Gender", gender));
                }

                if (!string.IsNullOrEmpty(household))
                {
                    whereClauses.Add("h.Name = @Household");
                    parameters.Add(new SqlParameter("@Household", household));
                }

                // Add active member filter
                whereClauses.Add("m.IsActive = 1");

                var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                var query = @$"
                    SELECT 
                        m.Id, m.FirstName, m.LastName, m.FullName, m.Gender, m.DateOfBirth, 
                        m.PhoneNumber, m.Email, m.Address, m.Occupation, m.MaritalStatus,
                        m.HouseholdId, m.RankInChurch, m.ImmigrationStatus, m.DocumentExpiry,
                        m.DateJoined, m.WelfareMemberId, m.IsFlagged, m.AbsentSince,
                        m.AdditionalNotes, m.CreatedAt, m.UpdatedAt,
                        h.Name as HouseholdName,
                        wm.FullName as WelfareMemberName
                    FROM Members m
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    LEFT JOIN Members wm ON m.WelfareMemberId = wm.Id
                    {whereClause}
                    ORDER BY m.FullName
                    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY";

                var countQuery = @$"
                    SELECT COUNT(*) as TotalCount 
                    FROM Members m
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    {whereClause}";

                // Get total count
                await using var countCommand = new SqlCommand(countQuery, connection);
                countCommand.Parameters.AddRange(parameters.ToArray());
                var totalCount = (int?)await countCommand.ExecuteScalarAsync() ?? 0;

                // Get members
                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());
                command.Parameters.Add(new SqlParameter("@Offset", offset));
                command.Parameters.Add(new SqlParameter("@Limit", limit));

                await using var reader = await command.ExecuteReaderAsync();
                var members = new List<MemberResponse>();

                while (await reader.ReadAsync())
                {
                    var member = new MemberResponse
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Gender = reader.GetString(reader.GetOrdinal("Gender")),
                        DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? null : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        HouseholdName = reader.IsDBNull(reader.GetOrdinal("HouseholdName")) ? null : reader.GetString(reader.GetOrdinal("HouseholdName")),
                        WelfareMemberName = reader.IsDBNull(reader.GetOrdinal("WelfareMemberName")) ? null : reader.GetString(reader.GetOrdinal("WelfareMemberName")),
                        IsFlagged = reader.GetBoolean(reader.GetOrdinal("IsFlagged")),
                        AbsentSince = reader.IsDBNull(reader.GetOrdinal("AbsentSince")) ? null : reader.GetDateTime(reader.GetOrdinal("AbsentSince")),
                        ImmigrationStatus = reader.IsDBNull(reader.GetOrdinal("ImmigrationStatus")) ? null : reader.GetString(reader.GetOrdinal("ImmigrationStatus")),
                        DocumentExpiry = reader.IsDBNull(reader.GetOrdinal("DocumentExpiry")) ? null : reader.GetDateTime(reader.GetOrdinal("DocumentExpiry")),
                        DateJoined = reader.IsDBNull(reader.GetOrdinal("DateJoined")) ? null : reader.GetDateTime(reader.GetOrdinal("DateJoined"))
                    };
                    members.Add(member);
                }

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
                _logger.LogError(ex, "Failed to fetch members with search: {Search}", search);
                return StatusCode(500, new { error = "Failed to fetch members" });
            }
        }

        [HttpPost]
public async Task<IActionResult> CreateMember([FromBody] MemberCreateRequest request)
{
    if (request == null)
    {
        return BadRequest(new { error = "Request body is required" });
    }

    if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
    {
        return BadRequest(new { error = "First name and last name are required" });
    }

    await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
    
    try
    {
        await connection.OpenAsync();

        var query = @"
            INSERT INTO Members (
                FirstName, LastName, FullName, Gender, DateOfBirth, PhoneNumber, Email, 
                Address, Occupation, MaritalStatus, HouseholdId, RankInChurch, ImmigrationStatus,
                DocumentExpiry, DateJoined, WelfareMemberId, AdditionalNotes, IsActive, CreatedAt
            ) 
            OUTPUT INSERTED.Id, INSERTED.FirstName, INSERTED.LastName, INSERTED.FullName, 
                   INSERTED.Gender, INSERTED.Email, INSERTED.PhoneNumber, INSERTED.HouseholdId,
                   INSERTED.DateOfBirth, INSERTED.ImmigrationStatus, INSERTED.DateJoined
            VALUES (
                @FirstName, @LastName, @FullName, @Gender, @DateOfBirth, @PhoneNumber, @Email, 
                @Address, @Occupation, @MaritalStatus, @HouseholdId, @RankInChurch, @ImmigrationStatus,
                @DocumentExpiry, @DateJoined, @WelfareMemberId, @AdditionalNotes, 1, GETUTCDATE()
            )";

        await using var command = new SqlCommand(query, connection);

        // Calculate full name if not provided
        var fullName = string.IsNullOrWhiteSpace(request.FullName) 
            ? $"{request.FirstName.Trim()} {request.LastName.Trim()}" 
            : request.FullName.Trim();

        // Handle DateOfBirth - accept both DateTime and string
        DateTime? dateOfBirth = null;
        if (request.DateOfBirth != null)
        {
            if (request.DateOfBirth is DateTime dob)
            {
                dateOfBirth = dob;
            }
            else if (request.DateOfBirth is string dobString && !string.IsNullOrWhiteSpace(dobString))
            {
                if (DateTime.TryParse(dobString, out var parsedDob))
                {
                    dateOfBirth = parsedDob;
                }
                else
                {
                    _logger.LogWarning("Could not parse DateOfBirth string: {DateString}", dobString);
                }
            }
            else if (request.DateOfBirth is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var dateString = jsonElement.GetString();
                    if (DateTime.TryParse(dateString, out var parsedDob))
                    {
                        dateOfBirth = parsedDob;
                    }
                }
            }
        }

        // Add parameters
        command.Parameters.AddWithValue("@FirstName", request.FirstName.Trim());
        command.Parameters.AddWithValue("@LastName", request.LastName.Trim());
        command.Parameters.AddWithValue("@FullName", fullName);
        command.Parameters.AddWithValue("@Gender", request.Gender?.Trim() ?? "Unknown");
        command.Parameters.AddWithValue("@DateOfBirth", dateOfBirth ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Email", request.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Address", request.Address ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Occupation", request.Occupation ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@MaritalStatus", request.MaritalStatus ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@HouseholdId", request.HouseholdId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RankInChurch", request.RankInChurch ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ImmigrationStatus", request.ImmigrationStatus ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DocumentExpiry", request.DocumentExpiry ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DateJoined", request.DateJoined ?? DateTime.UtcNow);
        command.Parameters.AddWithValue("@WelfareMemberId", request.WelfareMemberId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@AdditionalNotes", request.AdditionalNotes ?? (object)DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var member = new
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Gender = reader.GetString(reader.GetOrdinal("Gender")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                ImmigrationStatus = reader.IsDBNull(reader.GetOrdinal("ImmigrationStatus")) ? null : reader.GetString(reader.GetOrdinal("ImmigrationStatus")),
                DateJoined = reader.IsDBNull(reader.GetOrdinal("DateJoined")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                HouseholdId = reader.IsDBNull(reader.GetOrdinal("HouseholdId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("HouseholdId"))
            };
        
            return StatusCode(201, member);
        }

        return BadRequest(new { error = "Failed to create member" });
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
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Only CSV files are allowed" });
            }

            if (file.Length > 25 * 1024 * 1024) // 25MB limit
            {
                return BadRequest(new { error = "File size exceeds 25MB limit" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();
                
                // Read CSV content
                using var stream = new StreamReader(file.OpenReadStream());
                var csvContent = await stream.ReadToEndAsync();
                
                var lines = csvContent.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                if (lines.Length <= 1)
                {
                    return BadRequest(new { error = "CSV file is empty or has no data rows" });
                }

                var headers = lines[0].Split(',').Select(h => h.Trim().ToLower()).ToArray();
                
                // Validate required columns
                var requiredColumns = new[] { "firstname", "lastname", "gender" };
                var missingColumns = requiredColumns.Where(rc => !headers.Contains(rc)).ToArray();
                
                if (missingColumns.Any())
                {
                    return BadRequest(new { error = $"Missing required columns: {string.Join(", ", missingColumns)}" });
                }

                var successCount = 0;
                var errorCount = 0;
                var errors = new List<string>();

                // Process each row
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
                        var dateOfBirth = GetValueByHeader(headers, values, "dateofbirth");
                        var address = GetValueByHeader(headers, values, "address");

                        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                        {
                            errorCount++;
                            errors.Add($"Row {i + 1}: First name and last name are required");
                            continue;
                        }

                        // Parse date if provided
                        DateTime? dob = null;
                        if (!string.IsNullOrWhiteSpace(dateOfBirth))
                        {
                            if (DateTime.TryParse(dateOfBirth, out var parsedDob))
                            {
                                dob = parsedDob;
                            }
                            else
                            {
                                errorCount++;
                                errors.Add($"Row {i + 1}: Invalid date format for Date of Birth");
                                continue;
                            }
                        }

                        // Insert member
                        var insertQuery = @"
                            INSERT INTO Members (
                                FirstName, LastName, FullName, Gender, DateOfBirth, PhoneNumber, Email, 
                                Address, IsActive, CreatedAt
                            )
                            VALUES (@FirstName, @LastName, @FullName, @Gender, @DateOfBirth, @PhoneNumber, 
                                    @Email, @Address, 1, GETUTCDATE())";

                        await using var command = new SqlCommand(insertQuery, connection);
                        command.Parameters.AddWithValue("@FirstName", firstName.Trim());
                        command.Parameters.AddWithValue("@LastName", lastName.Trim());
                        command.Parameters.AddWithValue("@FullName", $"{firstName.Trim()} {lastName.Trim()}");
                        command.Parameters.AddWithValue("@Gender", gender.Trim());
                        command.Parameters.AddWithValue("@DateOfBirth", dob ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrEmpty(phoneNumber) ? (object)DBNull.Value : phoneNumber.Trim());
                        command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email.Trim().ToLower());
                        command.Parameters.AddWithValue("@Address", string.IsNullOrEmpty(address) ? (object)DBNull.Value : address.Trim());

                        await command.ExecuteNonQueryAsync();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"Row {i + 1}: {ex.Message}");
                    }
                }

                return Ok(new 
                { 
                    message = $"CSV import completed. Success: {successCount}, Errors: {errorCount}",
                    successCount,
                    errorCount,
                    errors = errors.Take(10).ToArray() // Return first 10 errors
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
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid member ID" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        m.*,
                        h.Name as HouseholdName,
                        wm.FullName as WelfareMemberName
                    FROM Members m
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    LEFT JOIN Members wm ON m.WelfareMemberId = wm.Id
                    WHERE m.Id = @Id AND m.IsActive = 1";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                await using var reader = await command.ExecuteReaderAsync();
                
                if (!await reader.ReadAsync())
                {
                    return NotFound(new { error = "Member not found" });
                }

                var member = new MemberDetailResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Gender = reader.GetString(reader.GetOrdinal("Gender")),
                    DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? null : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? null : reader.GetString(reader.GetOrdinal("Address")),
                    Occupation = reader.IsDBNull(reader.GetOrdinal("Occupation")) ? null : reader.GetString(reader.GetOrdinal("Occupation")),
                    MaritalStatus = reader.IsDBNull(reader.GetOrdinal("MaritalStatus")) ? null : reader.GetString(reader.GetOrdinal("MaritalStatus")),
                    HouseholdId = reader.IsDBNull(reader.GetOrdinal("HouseholdId")) ? null : reader.GetInt32(reader.GetOrdinal("HouseholdId")),
                    HouseholdName = reader.IsDBNull(reader.GetOrdinal("HouseholdName")) ? null : reader.GetString(reader.GetOrdinal("HouseholdName")),
                    RankInChurch = reader.IsDBNull(reader.GetOrdinal("RankInChurch")) ? null : reader.GetString(reader.GetOrdinal("RankInChurch")),
                    ImmigrationStatus = reader.IsDBNull(reader.GetOrdinal("ImmigrationStatus")) ? null : reader.GetString(reader.GetOrdinal("ImmigrationStatus")),
                    DocumentExpiry = reader.IsDBNull(reader.GetOrdinal("DocumentExpiry")) ? null : reader.GetDateTime(reader.GetOrdinal("DocumentExpiry")),
                    DateJoined = reader.IsDBNull(reader.GetOrdinal("DateJoined")) ? null : reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                    WelfareMemberId = reader.IsDBNull(reader.GetOrdinal("WelfareMemberId")) ? null : reader.GetInt32(reader.GetOrdinal("WelfareMemberId")),
                    WelfareMemberName = reader.IsDBNull(reader.GetOrdinal("WelfareMemberName")) ? null : reader.GetString(reader.GetOrdinal("WelfareMemberName")),
                    IsFlagged = reader.GetBoolean(reader.GetOrdinal("IsFlagged")),
                    AbsentSince = reader.IsDBNull(reader.GetOrdinal("AbsentSince")) ? null : reader.GetDateTime(reader.GetOrdinal("AbsentSince")),
                    AdditionalNotes = reader.IsDBNull(reader.GetOrdinal("AdditionalNotes")) ? null : reader.GetString(reader.GetOrdinal("AdditionalNotes")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                };

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
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid member ID" });
            }

            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new { error = "First name and last name are required" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    UPDATE Members 
                    SET FirstName = @FirstName, LastName = @LastName, FullName = @FullName, 
                        Gender = @Gender, DateOfBirth = @DateOfBirth, PhoneNumber = @PhoneNumber, 
                        Email = @Email, Address = @Address, Occupation = @Occupation, 
                        MaritalStatus = @MaritalStatus, HouseholdId = @HouseholdId,
                        RankInChurch = @RankInChurch, ImmigrationStatus = @ImmigrationStatus,
                        DocumentExpiry = @DocumentExpiry, DateJoined = @DateJoined,
                        WelfareMemberId = @WelfareMemberId, AdditionalNotes = @AdditionalNotes,
                        IsFlagged = @IsFlagged, AbsentSince = @AbsentSince,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id AND IsActive = 1";

                await using var command = new SqlCommand(query, connection);
                
                // Calculate full name if not provided
                var fullName = string.IsNullOrWhiteSpace(request.FullName) 
                    ? $"{request.FirstName.Trim()} {request.LastName.Trim()}" 
                    : request.FullName.Trim();

                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@FirstName", request.FirstName.Trim());
                command.Parameters.AddWithValue("@LastName", request.LastName.Trim());
                command.Parameters.AddWithValue("@FullName", fullName);
                command.Parameters.AddWithValue("@Gender", request.Gender?.Trim() ?? "Unknown");
                command.Parameters.AddWithValue("@DateOfBirth", request.DateOfBirth ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PhoneNumber", request.PhoneNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Email", request.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Address", request.Address ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Occupation", request.Occupation ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MaritalStatus", request.MaritalStatus ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@HouseholdId", request.HouseholdId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@RankInChurch", request.RankInChurch ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ImmigrationStatus", request.ImmigrationStatus ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DocumentExpiry", request.DocumentExpiry ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DateJoined", request.DateJoined ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@WelfareMemberId", request.WelfareMemberId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AdditionalNotes", request.AdditionalNotes ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IsFlagged", request.IsFlagged);
                command.Parameters.AddWithValue("@AbsentSince", request.AbsentSince ?? (object)DBNull.Value);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    return NotFound(new { error = "Member not found" });
                }

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
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid member ID" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                // Soft delete - set IsActive = 0
                var query = @"
                    UPDATE Members 
                    SET IsActive = 0, UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id AND IsActive = 1";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    return NotFound(new { error = "Member not found" });
                }

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
