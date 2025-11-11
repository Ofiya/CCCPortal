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
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsersController> _logger;

        public ReportsController(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<UsersController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("attendance")]
        public async Task<IActionResult> GetAttendanceReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            // Set default date range to last 30 days if not provided
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
                return BadRequest(new { error = "Start date cannot be after end date" });

            if ((end - start).TotalDays > 365)
                return BadRequest(new { error = "Date range cannot exceed 1 year" });

            try
            {
                var attendanceData = await _dbContext.Attendance
                    .Where(a => a.ServiceDate >= start && a.ServiceDate <= end)
                    .GroupBy(a => a.ServiceDate)
                    .Select(g => new
                    {
                        ServiceDate = g.Key,
                        TotalMembers = g.Count(),
                        PresentCount = g.Count(a => a.IsPresent),
                        AbsentCount = g.Count() - g.Count(a => a.IsPresent),
                        AttendanceRate = Math.Round((double)g.Count(a => a.IsPresent) / g.Count() * 100, 2)
                    })
                    .OrderByDescending(x => x.ServiceDate)
                    .ToListAsync();

                return Ok(new
                {
                    startDate = start.ToString("yyyy-MM-dd"),
                    endDate = end.ToString("yyyy-MM-dd"),
                    totalRecords = attendanceData.Count,
                    data = attendanceData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report");
                return StatusCode(500, new { error = "Failed to generate attendance report" });
            }
        }

        [HttpGet("members")]
        public async Task<IActionResult> GetMembersReport()
        {
            try
            {
                var members = await _dbContext.Members
                    .Include(m => m.Household)
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.FullName)
                    .Select(m => new
                    {
                        m.FirstName,
                        m.LastName,
                        m.FullName,
                        m.Gender,
                        DateOfBirth = m.DateOfBirth.HasValue ? m.DateOfBirth.Value.ToString("yyyy-MM-dd") : null,
                        m.PhoneNumber,
                        m.Email,
                        HouseholdName = m.Household != null ? m.Household.Name : "No Household",
                        m.RankInChurch,
                        m.ImmigrationStatus,
                        DateJoined = m.DateJoined.HasValue ? m.DateJoined.Value.ToString("yyyy-MM-dd") : null,
                        MemberSince = m.CreatedAt.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    totalMembers = members.Count,
                    data = members
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating members report");
                return StatusCode(500, new { error = "Failed to generate members report" });
            }
        }

        [HttpGet("households")]
        public async Task<IActionResult> GetHouseholdsReport()
        {
            try
            {
                var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

                var households = await _dbContext.Households
                    .Where(h => h.IsActive)
                    .Include(h => h.HeadMember)
                    .Include(h => h.Members)
                    .Select(h => new
                    {
                        HouseholdName = h.Name,
                        h.Address,
                        h.PrimaryPhone,
                        h.Email,
                        HeadName = h.HeadMember != null ? h.HeadMember.FullName : "Not Assigned",
                        MemberCount = h.Members.Count(m => m.IsActive),
                        AvgAttendanceRate = Math.Round(
                            h.Members
                                .Where(m => m.IsActive)
                                .SelectMany(m => m.Attendance.Where(a => a.ServiceDate >= oneMonthAgo))
                                .DefaultIfEmpty()
                                .Average(a => a == null ? 0 : a.IsPresent ? 1.0 : 0.0) * 100, 2)
                    })
                    .OrderByDescending(h => h.MemberCount)
                    .ThenBy(h => h.HouseholdName)
                    .ToListAsync();

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    totalHouseholds = households.Count,
                    data = households
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating households report");
                return StatusCode(500, new { error = "Failed to generate households report" });
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var monthAgo = today.AddMonths(-1);

                // Total members
                var totalMembersTask = _dbContext.Members.CountAsync(m => m.IsActive);

                // Total households
                var totalHouseholdsTask = _dbContext.Households.CountAsync(h => h.IsActive);

                // Today's attendance
                var todayAttendanceQuery = _dbContext.Attendance.Where(a => a.ServiceDate == today);
                var todayTotalTask = todayAttendanceQuery.CountAsync();
                var todayPresentTask = todayAttendanceQuery.CountAsync(a => a.IsPresent);

                // Monthly attendance average
                var monthlyAttendanceRateTask = _dbContext.Attendance
                    .Where(a => a.ServiceDate >= monthAgo)
                    .GroupBy(a => 1)
                    .Select(g => g.Average(a => a.IsPresent ? 1.0 : 0.0) * 100)
                    .FirstOrDefaultAsync();

                // Gender distribution
                var genderDistributionTask = _dbContext.Members
                    .Where(m => m.IsActive && m.Gender != null)
                    .GroupBy(m => m.Gender)
                    .Select(g => new
                    {
                        Gender = g.Key ?? "Unknown",
                        Count = g.Count()
                    })
                    .ToListAsync();

                await Task.WhenAll(totalMembersTask, totalHouseholdsTask, todayTotalTask,
                    todayPresentTask, monthlyAttendanceRateTask, genderDistributionTask);

                var todayTotal = todayTotalTask.Result;
                var todayPresent = todayPresentTask.Result;
                var todayAbsent = todayTotal - todayPresent;

                var stats = new
                {
                    TotalMembers = totalMembersTask.Result,
                    TotalHouseholds = totalHouseholdsTask.Result,
                    TodayAttendance = new
                    {
                        TodayTotal = todayTotal,
                        TodayPresent = todayPresent,
                        TodayAbsent = todayAbsent
                    },
                    MonthlyAttendanceRate = Math.Round(monthlyAttendanceRateTask.Result, 2),
                    GenderDistribution = genderDistributionTask.Result
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
            try
            {
                var flaggedMembers = await _dbContext.Members
                    .Include(m => m.Household)
                    .Where(m => m.IsActive && m.IsFlagged)
                    .OrderByDescending(m => m.AbsentSince)
                    .Select(m => new
                    {
                        m.FirstName,
                        m.LastName,
                        m.FullName,
                        m.Gender,
                        m.PhoneNumber,
                        m.Email,
                        m.ImmigrationStatus,
                        m.AdditionalNotes,
                        HouseholdName = m.Household != null ? m.Household.Name : null,
                        AbsentSince = m.AbsentSince != null ? m.AbsentSince.Value.ToString("yyyy-MM-dd") : null,
                        DaysAbsent = m.AbsentSince != null ? EF.Functions.DateDiffDay(m.AbsentSince.Value, DateTime.UtcNow) : 0
                    })
                    .ToListAsync();

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    totalFlagged = flaggedMembers.Count,
                    data = flaggedMembers
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
            try
            {
                var today = DateTime.UtcNow.Date;
                var endDate = today.AddDays(days);

                // Get all members with valid DOB
                var members = await _dbContext.Members
                    .Include(m => m.Household)
                    .Where(m => m.IsActive && m.DateOfBirth.HasValue)
                    .ToListAsync();

                // Filter upcoming birthdays
                var upcomingBirthdays = members
                    .Select(m =>
                    {
                        var dob = m.DateOfBirth!.Value;
                        var nextBirthday = new DateTime(today.Year, dob.Month, dob.Day);
                        if (nextBirthday < today)
                            nextBirthday = nextBirthday.AddYears(1);

                        var daysUntil = (nextBirthday - today).Days;

                        return new
                        {
                            m.FirstName,
                            m.LastName,
                            m.FullName,
                            m.PhoneNumber,
                            m.Email,
                            m.DateOfBirth,
                            HouseholdName = m.Household?.Name,
                            DaysUntil = daysUntil,
                            NextBirthday = nextBirthday.ToString("yyyy-MM-dd")
                        };
                    })
                    .Where(x => x.DaysUntil <= days)
                    .OrderBy(x => x.DaysUntil)
                    .ToList();

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    daysRange = days,
                    totalUpcoming = upcomingBirthdays.Count,
                    data = upcomingBirthdays
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
            try
            {
                var today = DateTime.UtcNow.Date;
                var endDate = today.AddDays(days);

                var immigrationReport = await _dbContext.Members
                    .Include(m => m.Household)
                    .Where(m => m.IsActive && m.DocumentExpiry != null
                        && m.DocumentExpiry >= today && m.DocumentExpiry <= endDate)
                    .OrderBy(m => m.DocumentExpiry)
                    .Select(m => new
                    {
                        m.FirstName,
                        m.LastName,
                        m.FullName,
                        m.Gender,
                        m.PhoneNumber,
                        m.Email,
                        m.ImmigrationStatus,
                        HouseholdName = m.Household != null ? m.Household.Name : null,
                        DocumentExpiry = m.DocumentExpiry!.Value.ToString("yyyy-MM-dd"),
                        DaysUntilExpiry = EF.Functions.DateDiffDay(today, m.DocumentExpiry!.Value)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    daysRange = days,
                    totalExpiring = immigrationReport.Count,
                    data = immigrationReport
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
