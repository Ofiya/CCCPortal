using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IConfiguration configuration, ILogger<ReportsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("attendance")]
        public async Task<IActionResult> GetAttendanceReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            // Set default date range to last 30 days if not provided
            var defaultStartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
            var defaultEndDate = endDate ?? DateTime.UtcNow;

            // Validate date range
            if (defaultStartDate > defaultEndDate)
            {
                return BadRequest(new { error = "Start date cannot be after end date" });
            }

            if ((defaultEndDate - defaultStartDate).TotalDays > 365)
            {
                return BadRequest(new { error = "Date range cannot exceed 1 year" });
            }

            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        ServiceDate,
                        COUNT(*) as TotalMembers,
                        SUM(CASE WHEN IsPresent = 1 THEN 1 ELSE 0 END) as PresentCount,
                        AVG(CASE WHEN IsPresent = 1 THEN 1.0 ELSE 0.0 END) * 100 as AttendanceRate
                    FROM Attendance 
                    WHERE ServiceDate BETWEEN @StartDate AND @EndDate
                    GROUP BY ServiceDate
                    ORDER BY ServiceDate DESC";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StartDate", defaultStartDate.Date);
                command.Parameters.AddWithValue("@EndDate", defaultEndDate.Date);
                
                await using var reader = await command.ExecuteReaderAsync();
                var report = new List<object>();

                while (await reader.ReadAsync())
                {
                    report.Add(new
                    {
                        ServiceDate = reader.GetDateTime(reader.GetOrdinal("ServiceDate")).ToString("yyyy-MM-dd"),
                        TotalMembers = reader.GetInt32(reader.GetOrdinal("TotalMembers")),
                        PresentCount = reader.GetInt32(reader.GetOrdinal("PresentCount")),
                        AbsentCount = reader.GetInt32(reader.GetOrdinal("TotalMembers")) - reader.GetInt32(reader.GetOrdinal("PresentCount")),
                        AttendanceRate = Math.Round(reader.GetDouble(reader.GetOrdinal("AttendanceRate")), 2)
                    });
                }

                return Ok(new
                {
                    startDate = defaultStartDate.ToString("yyyy-MM-dd"),
                    endDate = defaultEndDate.ToString("yyyy-MM-dd"),
                    totalRecords = report.Count,
                    data = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate attendance report from {StartDate} to {EndDate}", 
                    defaultStartDate, defaultEndDate);
                return StatusCode(500, new { error = "Failed to generate attendance report" });
            }
        }

        [HttpGet("members")]
        public async Task<IActionResult> GetMembersReport()
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        m.FirstName, m.LastName, m.FullName, m.Gender, m.DateOfBirth, m.PhoneNumber, m.Email, 
                        h.Name as HouseholdName, m.RankInChurch, m.ImmigrationStatus, 
                        m.DateJoined, m.CreatedAt
                    FROM Members m
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    WHERE m.IsActive = 1
                    ORDER BY m.FullName";

                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();
                var report = new List<object>();

                while (await reader.ReadAsync())
                {
                    report.Add(new
                    {
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Gender = reader.GetString(reader.GetOrdinal("Gender")),
                        DateOfBirth = reader.IsDBNull(reader.GetOrdinal("DateOfBirth")) ? 
                            null : reader.GetDateTime(reader.GetOrdinal("DateOfBirth")).ToString("yyyy-MM-dd"),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        HouseholdName = reader.IsDBNull(reader.GetOrdinal("HouseholdName")) ? "No Household" : reader.GetString(reader.GetOrdinal("HouseholdName")),
                        RankInChurch = reader.IsDBNull(reader.GetOrdinal("RankInChurch")) ? null : reader.GetString(reader.GetOrdinal("RankInChurch")),
                        ImmigrationStatus = reader.IsDBNull(reader.GetOrdinal("ImmigrationStatus")) ? null : reader.GetString(reader.GetOrdinal("ImmigrationStatus")),
                        DateJoined = reader.IsDBNull(reader.GetOrdinal("DateJoined")) ? 
                            null : reader.GetDateTime(reader.GetOrdinal("DateJoined")).ToString("yyyy-MM-dd"),
                        MemberSince = reader.GetDateTime(reader.GetOrdinal("CreatedAt")).ToString("yyyy-MM-dd")
                    });
                }

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    totalMembers = report.Count,
                    data = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate members report");
                return StatusCode(500, new { error = "Failed to generate members report" });
            }
        }

        [HttpGet("households")]
        public async Task<IActionResult> GetHouseholdsReport()
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        h.Name as HouseholdName,
                        h.Address,
                        h.PrimaryPhone,
                        h.Email,
                        hm.FullName as HeadName,
                        COUNT(m.Id) as MemberCount,
                        AVG(CASE WHEN a.IsPresent = 1 THEN 1.0 ELSE 0.0 END) * 100 as AvgAttendanceRate
                    FROM Households h
                    LEFT JOIN Members hm ON h.HeadMemberId = hm.Id
                    LEFT JOIN Members m ON h.Id = m.HouseholdId AND m.IsActive = 1
                    LEFT JOIN Attendance a ON m.Id = a.MemberId AND a.ServiceDate >= DATEADD(month, -1, GETUTCDATE())
                    WHERE h.IsActive = 1
                    GROUP BY h.Id, h.Name, h.Address, h.PrimaryPhone, h.Email, hm.FullName
                    ORDER BY MemberCount DESC, h.Name";

                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();
                var report = new List<object>();

                while (await reader.ReadAsync())
                {
                    report.Add(new
                    {
                        HouseholdName = reader.GetString(reader.GetOrdinal("HouseholdName")),
                        Address = reader.GetString(reader.GetOrdinal("Address")),
                        PrimaryPhone = reader.GetString(reader.GetOrdinal("PrimaryPhone")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        HeadName = reader.IsDBNull(reader.GetOrdinal("HeadName")) ? "Not Assigned" : reader.GetString(reader.GetOrdinal("HeadName")),
                        MemberCount = reader.GetInt32(reader.GetOrdinal("MemberCount")),
                        AvgAttendanceRate = reader.IsDBNull(reader.GetOrdinal("AvgAttendanceRate")) ? 0 : Math.Round(reader.GetDouble(reader.GetOrdinal("AvgAttendanceRate")), 2)
                    });
                }

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    totalHouseholds = report.Count,
                    data = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate households report");
                return StatusCode(500, new { error = "Failed to generate households report" });
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    -- Total active members
                    SELECT COUNT(*) as TotalMembers FROM Members WHERE IsActive = 1;
                    
                    -- Total households
                    SELECT COUNT(*) as TotalHouseholds FROM Households WHERE IsActive = 1;
                    
                    -- Today's attendance
                    SELECT 
                        COUNT(*) as TodayTotal,
                        SUM(CASE WHEN IsPresent = 1 THEN 1 ELSE 0 END) as TodayPresent
                    FROM Attendance 
                    WHERE ServiceDate = CAST(GETUTCDATE() AS DATE);
                    
                    -- Monthly attendance average
                    SELECT AVG(CASE WHEN IsPresent = 1 THEN 1.0 ELSE 0.0 END) * 100 as MonthlyAttendance
                    FROM Attendance 
                    WHERE ServiceDate >= DATEADD(month, -1, GETUTCDATE());
                    
                    -- Members by gender
                    SELECT Gender, COUNT(*) as Count 
                    FROM Members 
                    WHERE IsActive = 1 
                    GROUP BY Gender;";

                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();

                // Read first result set: TotalMembers
                int totalMembers = 0;
                if (await reader.ReadAsync())
                {
                    totalMembers = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                }
                await reader.NextResultAsync();

                // Read second result set: TotalHouseholds
                int totalHouseholds = 0;
                if (await reader.ReadAsync())
                {
                    totalHouseholds = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                }
                await reader.NextResultAsync();

                // Read third result set: Today's attendance
                var todayAttendance = new Dictionary<string, object>();
                if (await reader.ReadAsync())
                {
                    var todayTotal = reader.IsDBNull(reader.GetOrdinal("TodayTotal")) ? 0 : reader.GetInt32(reader.GetOrdinal("TodayTotal"));
                    var todayPresent = reader.IsDBNull(reader.GetOrdinal("TodayPresent")) ? 0 : reader.GetInt32(reader.GetOrdinal("TodayPresent"));
                    
                    todayAttendance["TodayTotal"] = todayTotal;
                    todayAttendance["TodayPresent"] = todayPresent;
                    todayAttendance["TodayAbsent"] = todayTotal - todayPresent;
                }
                await reader.NextResultAsync();

                // Read fourth result set: Monthly attendance rate
                double monthlyAttendanceRate = 0;
                if (await reader.ReadAsync() && !reader.IsDBNull(0))
                {
                    monthlyAttendanceRate = Math.Round(reader.GetDouble(0), 2);
                }
                await reader.NextResultAsync();

                // Read fifth result set: Gender distribution
                var genderDistribution = new List<object>();
                while (await reader.ReadAsync())
                {
                    genderDistribution.Add(new
                    {
                        Gender = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
                        Count = reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
                    });
                }

                var stats = new
                {
                    TotalMembers = totalMembers,
                    TotalHouseholds = totalHouseholds,
                    TodayAttendance = todayAttendance,
                    MonthlyAttendanceRate = monthlyAttendanceRate,
                    GenderDistribution = genderDistribution
                };

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate dashboard stats");
                return StatusCode(500, new { error = "Failed to generate dashboard statistics" });
            }
        }

        [HttpGet("flagged")]
        public async Task<IActionResult> GetFlaggedMembersReport()
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        m.FirstName, m.LastName, m.FullName, m.Gender, m.PhoneNumber, m.Email,
                        m.AbsentSince, m.ImmigrationStatus, m.AdditionalNotes,
                        h.Name as HouseholdName,
                        DATEDIFF(day, m.AbsentSince, GETUTCDATE()) as DaysAbsent
                    FROM Members m
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    WHERE m.IsFlagged = 1 AND m.IsActive = 1
                    ORDER BY m.AbsentSince DESC";

                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();
                var report = new List<object>();

                while (await reader.ReadAsync())
                {
                    report.Add(new
                    {
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Gender = reader.GetString(reader.GetOrdinal("Gender")),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        HouseholdName = reader.IsDBNull(reader.GetOrdinal("HouseholdName")) ? null : reader.GetString(reader.GetOrdinal("HouseholdName")),
                        AbsentSince = reader.IsDBNull(reader.GetOrdinal("AbsentSince")) ? null : reader.GetDateTime(reader.GetOrdinal("AbsentSince")).ToString("yyyy-MM-dd"),
                        DaysAbsent = reader.IsDBNull(reader.GetOrdinal("DaysAbsent")) ? 0 : reader.GetInt32(reader.GetOrdinal("DaysAbsent")),
                        ImmigrationStatus = reader.IsDBNull(reader.GetOrdinal("ImmigrationStatus")) ? null : reader.GetString(reader.GetOrdinal("ImmigrationStatus")),
                        AdditionalNotes = reader.IsDBNull(reader.GetOrdinal("AdditionalNotes")) ? null : reader.GetString(reader.GetOrdinal("AdditionalNotes"))
                    });
                }

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    totalFlagged = report.Count,
                    data = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate flagged members report");
                return StatusCode(500, new { error = "Failed to generate flagged members report" });
            }
        }

        [HttpGet("birthdays")]
        public async Task<IActionResult> GetBirthdaysReport([FromQuery] int days = 30)
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        m.FirstName, m.LastName, m.FullName, m.DateOfBirth, m.PhoneNumber, m.Email,
                        h.Name as HouseholdName,
                        DATEDIFF(day, GETUTCDATE(), 
                            DATEFROMPARTS(
                                YEAR(GETUTCDATE()) + 
                                CASE WHEN MONTH(DateOfBirth) < MONTH(GETUTCDATE()) 
                                     OR (MONTH(DateOfBirth) = MONTH(GETUTCDATE()) 
                                     AND DAY(DateOfBirth) < DAY(GETUTCDATE())) 
                                THEN 1 ELSE 0 END,
                                MONTH(DateOfBirth), 
                                DAY(DateOfBirth)
                            )
                        ) as DaysUntilBirthday
                    FROM Members m
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    WHERE m.IsActive = 1 
                    AND m.DateOfBirth IS NOT NULL
                    AND DATEDIFF(day, GETUTCDATE(), 
                        DATEFROMPARTS(
                            YEAR(GETUTCDATE()) + 
                            CASE WHEN MONTH(DateOfBirth) < MONTH(GETUTCDATE()) 
                                 OR (MONTH(DateOfBirth) = MONTH(GETUTCDATE()) 
                                 AND DAY(DateOfBirth) < DAY(GETUTCDATE())) 
                            THEN 1 ELSE 0 END,
                            MONTH(DateOfBirth), 
                            DAY(DateOfBirth)
                        )
                    ) BETWEEN 0 AND @Days
                    ORDER BY DaysUntilBirthday";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Days", days);

                await using var reader = await command.ExecuteReaderAsync();
                var report = new List<object>();

                while (await reader.ReadAsync())
                {
                    report.Add(new
                    {
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")).ToString("yyyy-MM-dd"),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        HouseholdName = reader.IsDBNull(reader.GetOrdinal("HouseholdName")) ? null : reader.GetString(reader.GetOrdinal("HouseholdName")),
                        DaysUntil = reader.GetInt32(reader.GetOrdinal("DaysUntilBirthday")),
                        NextBirthday = DateTime.Now.AddDays(reader.GetInt32(reader.GetOrdinal("DaysUntilBirthday"))).ToString("yyyy-MM-dd")
                    });
                }

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    daysRange = days,
                    totalUpcoming = report.Count,
                    data = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate birthdays report");
                return StatusCode(500, new { error = "Failed to generate birthdays report" });
            }
        }

        [HttpGet("immigration")]
        public async Task<IActionResult> GetImmigrationReport([FromQuery] int days = 90)
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        m.FirstName, m.LastName, m.FullName, m.Gender, m.PhoneNumber, m.Email,
                        m.ImmigrationStatus, m.DocumentExpiry,
                        h.Name as HouseholdName,
                        DATEDIFF(day, GETUTCDATE(), m.DocumentExpiry) as DaysUntilExpiry
                    FROM Members m
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    WHERE m.IsActive = 1 
                    AND m.DocumentExpiry IS NOT NULL
                    AND m.DocumentExpiry >= GETUTCDATE()
                    AND m.DocumentExpiry <= DATEADD(day, @Days, GETUTCDATE())
                    ORDER BY m.DocumentExpiry ASC";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Days", days);

                await using var reader = await command.ExecuteReaderAsync();
                var report = new List<object>();

                while (await reader.ReadAsync())
                {
                    report.Add(new
                    {
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        Gender = reader.GetString(reader.GetOrdinal("Gender")),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        HouseholdName = reader.IsDBNull(reader.GetOrdinal("HouseholdName")) ? null : reader.GetString(reader.GetOrdinal("HouseholdName")),
                        ImmigrationStatus = reader.IsDBNull(reader.GetOrdinal("ImmigrationStatus")) ? null : reader.GetString(reader.GetOrdinal("ImmigrationStatus")),
                        DocumentExpiry = reader.GetDateTime(reader.GetOrdinal("DocumentExpiry")).ToString("yyyy-MM-dd"),
                        DaysUntilExpiry = reader.GetInt32(reader.GetOrdinal("DaysUntilExpiry"))
                    });
                }

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    daysRange = days,
                    totalExpiring = report.Count,
                    data = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate immigration report");
                return StatusCode(500, new { error = "Failed to generate immigration report" });
            }
        }
    }

    // Extension methods for simplified data access (optional - for simpler queries)
    public static class SqlDataReaderExtensions
    {
        public static async Task<T?> ReadSingleValueAsync<T>(this SqlDataReader reader)
        {
            if (await reader.ReadAsync())
            {
                var value = reader.GetValue(0);
                return value != DBNull.Value ? (T)value : default(T);
            }
            return default(T);
        }

        public static async Task<Dictionary<string, object>?> ReadSingleRowAsync(this SqlDataReader reader)
        {
            if (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                return row;
            }
            return null;
        }

        public static async Task<List<Dictionary<string, object>>?> ReadToListAsync(this SqlDataReader reader)
        {
            var list = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                list.Add(row);
            }
            return list.Count > 0 ? list : null;
        }
    }
}
