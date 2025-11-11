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
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<DashboardController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                var today = utcNow.Date;

                // Total active members
                var totalMembers = await _dbContext.Members.CountAsync(m => m.IsActive);

                // Attendance rate (last 30 days)
                var thirtyDaysAgo = utcNow.AddDays(-30);
                var recentAttendance = await _dbContext.Attendance
                    .Where(a => a.ServiceDate >= thirtyDaysAgo)
                    .ToListAsync();

                var totalRecords = recentAttendance.Select(a => a.MemberId).Distinct().Count();
                var presentCount = recentAttendance.Count(a => a.IsPresent);
                var attendanceRate = totalRecords > 0 ? Math.Round((presentCount / (double)totalRecords) * 100, 2) : 0;

                // Flagged members
                var flaggedMembers = await _dbContext.Members.CountAsync(m => m.IsFlagged && m.IsActive);

                // Expiring documents (next 90 days)
                var ninetyDays = utcNow.AddDays(90);
                var expiringDocuments = await _dbContext.Members
                    .CountAsync(m => m.DocumentExpiry != null &&
                                     m.DocumentExpiry >= utcNow &&
                                     m.DocumentExpiry <= ninetyDays &&
                                     m.IsActive);

                // Today's attendance
                var todayAttendance = await _dbContext.Attendance
                    .Where(a => a.ServiceDate == today)
                    .ToListAsync();

                var todayTotal = todayAttendance.Count;
                var todayPresent = todayAttendance.Count(a => a.IsPresent);

                // Gender distribution
                var genderDistribution = await _dbContext.Members
                    .Where(m => m.IsActive)
                    .GroupBy(m => m.Gender)
                    .Select(g => new
                    {
                        Gender = g.Key ?? "Unknown",
                        Count = g.Count()
                    })
                    .ToListAsync();

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
            try
            {
                var utcNow = DateTime.UtcNow.Date;

                var birthdays = await _dbContext.Members
                    .Where(m => m.IsActive && m.DateOfBirth != null)
                    .Select(m => new
                    {
                        m.Id,
                        m.FirstName,
                        m.LastName,
                        m.FullName,
                        m.DateOfBirth,
                        m.PhoneNumber,
                        m.Email,
                        DaysUntil = EF.Functions.DateDiffDay(
                            utcNow,
                            new DateTime(
                                utcNow.Year +
                                ((m.DateOfBirth!.Value.Month < utcNow.Month ||
                                 (m.DateOfBirth.Value.Month == utcNow.Month &&
                                  m.DateOfBirth.Value.Day < utcNow.Day)) ? 1 : 0),
                                m.DateOfBirth.Value.Month,
                                m.DateOfBirth.Value.Day))
                    })
                    .Where(x => x.DaysUntil >= 0 && x.DaysUntil <= days)
                    .OrderBy(x => x.DaysUntil)
                    .ToListAsync();

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
            try
            {
                var utcNow = DateTime.UtcNow;

                // Members needing follow-up (flagged)
                var flaggedMembers = await _dbContext.Members
                    .Include(m => m.Household)
                    .Where(m => m.IsFlagged && m.IsActive)
                    .Select(m => new
                    {
                        m.Id,
                        m.FirstName,
                        m.LastName,
                        m.FullName,
                        m.PhoneNumber,
                        m.Email,
                        HouseholdName = m.Household != null ? m.Household.Name : null,
                        m.AbsentSince,
                        DaysAbsent = m.AbsentSince != null ? EF.Functions.DateDiffDay(m.AbsentSince, utcNow) : 0,
                        m.AdditionalNotes
                    })
                    .OrderByDescending(m => m.AbsentSince)
                    .ToListAsync();

                // Recent follow-ups (last 7 days)
                var sevenDaysAgo = utcNow.AddDays(-7);
                var recentFollowups = await _dbContext.MemberFollowUps
                    .CountAsync(f => f.FollowUpDate >= sevenDaysAgo);

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