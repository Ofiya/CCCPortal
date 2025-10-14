using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(IConfiguration configuration, ILogger<AttendanceController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendance(
            [FromQuery] DateTime? date = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string? search = null)
        {
            // Validate input
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 10;

            var serviceDate = date ?? DateTime.UtcNow.Date;
            var offset = (page - 1) * limit;

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var whereClauses = new List<string> { "m.IsActive = 1" };
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ServiceDate", serviceDate),
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@Limit", limit)
                };

                if (!string.IsNullOrEmpty(search))
                {
                    whereClauses.Add("(m.FullName LIKE '%' + @Search + '%' OR m.FirstName LIKE '%' + @Search + '%' OR m.LastName LIKE '%' + @Search + '%')");
                    parameters.Add(new SqlParameter("@Search", search));
                }

                var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                var query = @$"
                    SELECT 
                        m.Id, m.FirstName, m.LastName, m.FullName, m.Gender, m.PhoneNumber,
                        h.Name as HouseholdName,
                        ISNULL(a.IsPresent, 0) as IsPresent,
                        ISNULL(a.IsFlagged, 0) as IsFlagged,
                        ISNULL(a.Notes, '') as Notes,
                        a.ServiceDate
                    FROM Members m
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    LEFT JOIN Attendance a ON m.Id = a.MemberId AND a.ServiceDate = @ServiceDate
                    {whereClause}
                    ORDER BY m.FullName
                    OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY";

                var countQuery = @$"SELECT COUNT(*) as TotalCount FROM Members m {whereClause}";

                // Get total count
                await using var countCmd = new SqlCommand(countQuery, connection);
                countCmd.Parameters.AddRange(parameters.Where(p => p.ParameterName != "@Offset" && p.ParameterName != "@Limit").ToArray());
                var totalCount = (int?)await countCmd.ExecuteScalarAsync() ?? 0;

                // Get attendance records
                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());
                
                await using var reader = await command.ExecuteReaderAsync();
                var attendance = new List<object>();

                while (await reader.ReadAsync())
                {
                    attendance.Add(new
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Gender = reader.GetString(reader.GetOrdinal("Gender")),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        HouseholdName = reader.IsDBNull(reader.GetOrdinal("HouseholdName")) ? null : reader.GetString(reader.GetOrdinal("HouseholdName")),
                        IsPresent = reader.GetBoolean(reader.GetOrdinal("IsPresent")),
                        IsFlagged = reader.GetBoolean(reader.GetOrdinal("IsFlagged")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        ServiceDate = reader.IsDBNull(reader.GetOrdinal("ServiceDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ServiceDate"))
                    });
                }

                return Ok(new
                {
                    attendance,
                    serviceDate = serviceDate.ToString("yyyy-MM-dd"),
                    totalCount,
                    currentPage = page,
                    totalPages = (int)Math.Ceiling(totalCount / (double)limit)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch attendance for date {ServiceDate}", serviceDate);
                return StatusCode(500, new { error = "Failed to fetch attendance data" });
            }
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> RecordBulkAttendance([FromBody] BulkAttendanceRequest request)
        {
            // Validate request
            if (request?.Attendance == null || !request.Attendance.Any())
            {
                return BadRequest(new { error = "Attendance data is required" });
            }

            if (!DateTime.TryParse(request.ServiceDate, out var serviceDate))
            {
                return BadRequest(new { error = "Invalid service date format" });
            }

            var recordedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(recordedBy) || !int.TryParse(recordedBy, out int recordedById))
            {
                return Unauthorized(new { error = "Invalid user identity" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();
                await using var transaction = await connection.BeginTransactionAsync();

                // Auto-flag members who have missed 3 consecutive services
                await AutoFlagMembersForFollowUp(connection, (SqlTransaction)transaction, serviceDate);

                foreach (var record in request.Attendance)
                {
                    var query = @"
                        MERGE Attendance AS target
                        USING (VALUES (@MemberId, @ServiceDate, @IsPresent, @IsFlagged, @Notes, @RecordedBy)) 
                        AS source (MemberId, ServiceDate, IsPresent, IsFlagged, Notes, RecordedBy)
                        ON target.MemberId = source.MemberId AND target.ServiceDate = source.ServiceDate
                        WHEN MATCHED THEN
                            UPDATE SET 
                                IsPresent = source.IsPresent, 
                                IsFlagged = source.IsFlagged, 
                                Notes = source.Notes, 
                                RecordedBy = source.RecordedBy,
                                UpdatedAt = GETUTCDATE()
                        WHEN NOT MATCHED THEN
                            INSERT (MemberId, ServiceDate, IsPresent, IsFlagged, Notes, RecordedBy, CreatedAt)
                            VALUES (source.MemberId, source.ServiceDate, source.IsPresent, 
                                    source.IsFlagged, source.Notes, source.RecordedBy, GETUTCDATE());";

                    await using var command = new SqlCommand(query, connection, (SqlTransaction)transaction);
                    command.Parameters.AddWithValue("@MemberId", record.MemberId);
                    command.Parameters.AddWithValue("@ServiceDate", serviceDate);
                    command.Parameters.AddWithValue("@IsPresent", record.IsPresent);
                    command.Parameters.AddWithValue("@IsFlagged", record.IsFlagged);
                    command.Parameters.AddWithValue("@Notes", record.Notes ?? string.Empty);
                    command.Parameters.AddWithValue("@RecordedBy", recordedById);

                    await command.ExecuteNonQueryAsync();

                    // Update member's IsFlagged status and AbsentSince date
                    await UpdateMemberFlagStatus(connection, (SqlTransaction)transaction, record.MemberId, record.IsFlagged, serviceDate, record.IsPresent);
                }

                await transaction.CommitAsync();
                return Ok(new { message = "Attendance recorded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record bulk attendance for date {ServiceDate}", request.ServiceDate);
                return StatusCode(500, new { error = "Failed to record attendance", details = ex.Message });
            }
        }

        private async Task AutoFlagMembersForFollowUp(SqlConnection connection, SqlTransaction transaction, DateTime serviceDate)
        {
            try
            {
                // Find members who have missed 3 consecutive services
                var query = @"
                    ;WITH ConsecutiveAbsences AS (
                        SELECT 
                            MemberId,
                            ServiceDate,
                            IsPresent,
                            LAG(IsPresent, 1) OVER (PARTITION BY MemberId ORDER BY ServiceDate) as Prev1,
                            LAG(IsPresent, 2) OVER (PARTITION BY MemberId ORDER BY ServiceDate) as Prev2
                        FROM Attendance
                        WHERE ServiceDate >= DATEADD(day, -21, @ServiceDate) -- Last 3 weeks
                    ),
                    MembersToFlag AS (
                        SELECT DISTINCT MemberId
                        FROM ConsecutiveAbsences
                        WHERE ServiceDate = @ServiceDate
                        AND IsPresent = 0
                        AND Prev1 = 0
                        AND Prev2 = 0
                    )
                    UPDATE m
                    SET IsFlagged = 1, 
                        AbsentSince = ISNULL(m.AbsentSince, @ServiceDate),
                        UpdatedAt = GETUTCDATE()
                    FROM Members m
                    INNER JOIN MembersToFlag mtf ON m.Id = mtf.MemberId
                    WHERE m.IsActive = 1";

                await using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@ServiceDate", serviceDate);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-flag members for follow-up");
                // Don't throw, as this shouldn't break the main attendance recording
            }
        }

        private async Task UpdateMemberFlagStatus(SqlConnection connection, SqlTransaction transaction, int memberId, bool isFlagged, DateTime serviceDate, bool isPresent)
        {
            try
            {
                var query = @"
                    UPDATE Members 
                    SET IsFlagged = @IsFlagged,
                        AbsentSince = CASE 
                            WHEN @IsFlagged = 1 AND AbsentSince IS NULL THEN @ServiceDate
                            WHEN @IsPresent = 1 THEN NULL
                            ELSE AbsentSince 
                        END,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @MemberId AND IsActive = 1";

                await using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@MemberId", memberId);
                command.Parameters.AddWithValue("@IsFlagged", isFlagged);
                command.Parameters.AddWithValue("@IsPresent", isPresent);
                command.Parameters.AddWithValue("@ServiceDate", serviceDate);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update member flag status for member {MemberId}", memberId);
            }
        }

        [HttpGet("range")]
        public async Task<IActionResult> GetAttendanceByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? householdId = null)
        {
            // Validate date range
            if (startDate > endDate)
            {
                return BadRequest(new { error = "Start date cannot be after end date" });
            }

            if ((endDate - startDate).TotalDays > 365)
            {
                return BadRequest(new { error = "Date range cannot exceed 1 year" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var whereClause = "WHERE a.ServiceDate BETWEEN @StartDate AND @EndDate";
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@StartDate", startDate.Date),
                    new SqlParameter("@EndDate", endDate.Date)
                };

                if (householdId.HasValue && householdId > 0)
                {
                    whereClause += " AND m.HouseholdId = @HouseholdId";
                    parameters.Add(new SqlParameter("@HouseholdId", householdId.Value));
                }

                var query = @$"
                    SELECT 
                        a.ServiceDate,
                        m.FirstName,
                        m.LastName,
                        m.FullName,
                        m.Gender,
                        h.Name as HouseholdName,
                        a.IsPresent,
                        a.IsFlagged,
                        a.Notes,
                        a.RecordedBy
                    FROM Attendance a
                    INNER JOIN Members m ON a.MemberId = m.Id
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    {whereClause}
                    ORDER BY a.ServiceDate DESC, m.FullName";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddRange(parameters.ToArray());
                
                await using var reader = await command.ExecuteReaderAsync();
                var attendance = new List<object>();

                while (await reader.ReadAsync())
                {
                    attendance.Add(new
                    {
                        ServiceDate = reader.GetDateTime(reader.GetOrdinal("ServiceDate")).ToString("yyyy-MM-dd"),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Gender = reader.GetString(reader.GetOrdinal("Gender")),
                        HouseholdName = reader.IsDBNull(reader.GetOrdinal("HouseholdName")) ? null : reader.GetString(reader.GetOrdinal("HouseholdName")),
                        IsPresent = reader.GetBoolean(reader.GetOrdinal("IsPresent")),
                        IsFlagged = reader.GetBoolean(reader.GetOrdinal("IsFlagged")),
                        Notes = reader.GetString(reader.GetOrdinal("Notes")),
                        RecordedBy = reader.GetString(reader.GetOrdinal("RecordedBy"))
                    });
                }

                return Ok(new
                {
                    startDate = startDate.ToString("yyyy-MM-dd"),
                    endDate = endDate.ToString("yyyy-MM-dd"),
                    householdId = householdId,
                    totalRecords = attendance.Count,
                    data = attendance
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch attendance from {StartDate} to {EndDate}", startDate, endDate);
                return StatusCode(500, new { error = "Failed to fetch attendance data" });
            }
        }
    }

    public class BulkAttendanceRequest
    {
        public string ServiceDate { get; set; } = string.Empty;
        public List<AttendanceRecord> Attendance { get; set; } = new();
    }

    public class AttendanceRecord
    {
        public int MemberId { get; set; }
        public bool IsPresent { get; set; }
        public bool IsFlagged { get; set; }
        public string? Notes { get; set; }
    }
}