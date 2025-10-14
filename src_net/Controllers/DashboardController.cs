using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IConfiguration configuration, ILogger<DashboardController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                // Total Members
                var totalMembersQuery = "SELECT COUNT(*) FROM Members WHERE IsActive = 1";
                await using var totalMembersCmd = new SqlCommand(totalMembersQuery, connection);
                var totalMembers = (int?)await totalMembersCmd.ExecuteScalarAsync() ?? 0;

                // Attendance Rate (last 30 days)
                var attendanceQuery = @"
                    SELECT 
                        COUNT(DISTINCT MemberId) as TotalRecords,
                        SUM(CASE WHEN IsPresent = 1 THEN 1 ELSE 0 END) as PresentCount
                    FROM Attendance 
                    WHERE ServiceDate >= DATEADD(day, -30, GETUTCDATE())";
                
                await using var attendanceCmd = new SqlCommand(attendanceQuery, connection);
                await using var attendanceReader = await attendanceCmd.ExecuteReaderAsync();
                
                double attendanceRate = 0;
                if (await attendanceReader.ReadAsync())
                {
                    var totalRecords = attendanceReader.GetInt32(attendanceReader.GetOrdinal("TotalRecords"));
                    var presentCount = attendanceReader.GetInt32(attendanceReader.GetOrdinal("PresentCount"));
                    attendanceRate = totalRecords > 0 ? Math.Round((presentCount / (double)totalRecords) * 100, 2) : 0;
                }
                await attendanceReader.CloseAsync();

                // Flagged Members (needing follow-up)
                var flaggedQuery = "SELECT COUNT(*) FROM Members WHERE IsFlagged = 1 AND IsActive = 1";
                await using var flaggedCmd = new SqlCommand(flaggedQuery, connection);
                var flaggedMembers = (int?)await flaggedCmd.ExecuteScalarAsync() ?? 0;

                // Expiring Documents (next 90 days)
                var expiringQuery = @"
                    SELECT COUNT(*) 
                    FROM Members 
                    WHERE DocumentExpiry IS NOT NULL 
                    AND DocumentExpiry <= DATEADD(day, 90, GETUTCDATE())
                    AND DocumentExpiry >= GETUTCDATE()
                    AND IsActive = 1";
                await using var expiringCmd = new SqlCommand(expiringQuery, connection);
                var expiringDocuments = (int?)await expiringCmd.ExecuteScalarAsync() ?? 0;

                // Today's attendance
                var todayAttendanceQuery = @"
                    SELECT 
                        COUNT(*) as TodayTotal,
                        SUM(CASE WHEN IsPresent = 1 THEN 1 ELSE 0 END) as TodayPresent
                    FROM Attendance 
                    WHERE ServiceDate = CAST(GETUTCDATE() AS DATE)";
                
                await using var todayAttendanceCmd = new SqlCommand(todayAttendanceQuery, connection);
                await using var todayReader = await todayAttendanceCmd.ExecuteReaderAsync();
                
                var todayTotal = 0;
                var todayPresent = 0;
                if (await todayReader.ReadAsync())
                {
                    todayTotal = todayReader.GetInt32(todayReader.GetOrdinal("TodayTotal"));
                    todayPresent = todayReader.GetInt32(todayReader.GetOrdinal("TodayPresent"));
                }
                await todayReader.CloseAsync();

                // Gender distribution
                var genderQuery = @"
                    SELECT Gender, COUNT(*) as Count 
                    FROM Members 
                    WHERE IsActive = 1 
                    GROUP BY Gender";
                
                await using var genderCmd = new SqlCommand(genderQuery, connection);
                await using var genderReader = await genderCmd.ExecuteReaderAsync();
                
                var genderDistribution = new List<object>();
                while (await genderReader.ReadAsync())
                {
                    genderDistribution.Add(new
                    {
                        Gender = genderReader.GetString(genderReader.GetOrdinal("Gender")),
                        Count = genderReader.GetInt32(genderReader.GetOrdinal("Count"))
                    });
                }

                return Ok(new
                {
                    totalMembers,
                    attendanceRate,
                    flaggedMembers,
                    expiringDocuments,
                    todayAttendance = new
                    {
                        total = todayTotal,
                        present = todayPresent,
                        absent = todayTotal - todayPresent
                    },
                    genderDistribution
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch dashboard statistics");
                return StatusCode(500, new { error = "Failed to fetch dashboard stats" });
            }
        }

        [HttpGet("birthdays")]
        public async Task<IActionResult> GetUpcomingBirthdays([FromQuery] int days = 30)
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        Id, FirstName, LastName, FullName, DateOfBirth, PhoneNumber, Email,
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
                    FROM Members 
                    WHERE IsActive = 1 
                    AND DateOfBirth IS NOT NULL
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
                var birthdays = new List<object>();

                while (await reader.ReadAsync())
                {
                    birthdays.Add(new
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName")),
                        DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                        DaysUntil = reader.GetInt32(reader.GetOrdinal("DaysUntilBirthday"))
                    });
                }

                return Ok(birthdays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch upcoming birthdays");
                return StatusCode(500, new { error = "Failed to fetch birthdays" });
            }
        }

        [HttpGet("welfare")]
        public async Task<IActionResult> GetWelfareDashboard()
        {
            await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            
            try
            {
                await connection.OpenAsync();

                // Members needing follow-up
                var flaggedQuery = @"
                    SELECT 
                        m.Id, m.FirstName, m.LastName, m.FullName, m.PhoneNumber, m.Email,
                        m.AbsentSince, m.AdditionalNotes,
                        h.Name as HouseholdName,
                        DATEDIFF(day, m.AbsentSince, GETUTCDATE()) as DaysAbsent
                    FROM Members m
                    LEFT JOIN Households h ON m.HouseholdId = h.Id
                    WHERE m.IsFlagged = 1 AND m.IsActive = 1
                    ORDER BY m.AbsentSince DESC";

                await using var flaggedCmd = new SqlCommand(flaggedQuery, connection);
                await using var flaggedReader = await flaggedCmd.ExecuteReaderAsync();
                
                var flaggedMembers = new List<object>();
                while (await flaggedReader.ReadAsync())
                {
                    flaggedMembers.Add(new
                    {
                        Id = flaggedReader.GetInt32(flaggedReader.GetOrdinal("Id")),
                        FirstName = flaggedReader.GetString(flaggedReader.GetOrdinal("FirstName")),
                        LastName = flaggedReader.GetString(flaggedReader.GetOrdinal("LastName")),
                        FullName = flaggedReader.GetString(flaggedReader.GetOrdinal("FullName")),
                        PhoneNumber = flaggedReader.IsDBNull(flaggedReader.GetOrdinal("PhoneNumber")) ? null : flaggedReader.GetString(flaggedReader.GetOrdinal("PhoneNumber")),
                        Email = flaggedReader.IsDBNull(flaggedReader.GetOrdinal("Email")) ? null : flaggedReader.GetString(flaggedReader.GetOrdinal("Email")),
                        HouseholdName = flaggedReader.IsDBNull(flaggedReader.GetOrdinal("HouseholdName")) ? null : flaggedReader.GetString(flaggedReader.GetOrdinal("HouseholdName")),
                        AbsentSince = flaggedReader.GetDateTime(flaggedReader.GetOrdinal("AbsentSince")),
                        DaysAbsent = flaggedReader.GetInt32(flaggedReader.GetOrdinal("DaysAbsent")),
                        AdditionalNotes = flaggedReader.IsDBNull(flaggedReader.GetOrdinal("AdditionalNotes")) ? null : flaggedReader.GetString(flaggedReader.GetOrdinal("AdditionalNotes"))
                    });
                }
                await flaggedReader.CloseAsync();

                // Recent follow-ups (last 7 days)
                var recentFollowupsQuery = @"
                    SELECT COUNT(*) as RecentCount
                    FROM MemberFollowUps 
                    WHERE FollowUpDate >= DATEADD(day, -7, GETUTCDATE())";
                
                await using var recentCmd = new SqlCommand(recentFollowupsQuery, connection);
                var recentFollowups = (int?)await recentCmd.ExecuteScalarAsync() ?? 0;

                return Ok(new
                {
                    flaggedMembers,
                    recentFollowups,
                    totalFlagged = flaggedMembers.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch welfare dashboard data");
                return StatusCode(500, new { error = "Failed to fetch welfare dashboard" });
            }
        }
    }
}