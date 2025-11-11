using MembershipAppBEAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace MembershipAppBEAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<AttendanceController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendance([FromQuery] DateTime? date = null, [FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? search = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var serviceDate = date ?? DateTime.UtcNow.Date;

                var query = _dbContext.Members
                    .Include(m => m.Household)
                    .Include(m => m.AttendanceRecords)
                    .Where(m => m.IsActive);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(m =>
                        m.FirstName.Contains(search) ||
                        m.LastName.Contains(search) ||
                        m.FullName.Contains(search));
                }

                var totalCount = await query.CountAsync();

                var results = await query
                    .OrderBy(x => x.FullName)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(x => new
                    {
                        x.Id,
                        x.FirstName,
                        x.LastName,
                        x.FullName,
                        x.Gender,
                        x.PhoneNumber,
                        HouseholdName = x.Household != null ? x.Household.Name : null,
                        Attendance = x.AttendanceRecords.FirstOrDefault(a => a.ServiceDate == serviceDate)
                    })
                    .ToListAsync();

                var attendance = results.Select(m => new
                {
                    m.Id,
                    m.FirstName,
                    m.LastName,
                    m.FullName,
                    m.Gender,
                    m.PhoneNumber,
                    m.HouseholdName,
                    IsPresent = m.Attendance?.IsPresent ?? false,
                    IsFlagged = m.Attendance?.IsFlagged ?? false,
                    Notes = m.Attendance?.Notes ?? string.Empty,
                    ServiceDate = m.Attendance?.ServiceDate
                }).ToList();

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
                _logger.LogError(ex, "Failed to fetch attendance for date {ServiceDate}", date);
                return StatusCode(500, new { error = "Failed to fetch attendance data" });
            }
        }

        [HttpPost("bulk")]
        public async Task<IActionResult> RecordBulkAttendance([FromBody] BulkAttendanceRequest request)
        {
            // Validate request
            if (request?.Attendance == null || !request.Attendance.Any())
                return BadRequest(new { error = "Attendance data is required" });

            if (!DateTime.TryParse(request.ServiceDate, out var serviceDate))
                return BadRequest(new { error = "Invalid service date format" });

            var recordedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(recordedBy) || !int.TryParse(recordedBy, out int recordedById))
                return Unauthorized(new { error = "Invalid user identity" });

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                await AutoFlagMembersForFollowUp(serviceDate);

                foreach (var record in request.Attendance)
                {
                    var existing = await _dbContext.Attendance
                        .FirstOrDefaultAsync(a => a.MemberId == record.MemberId && a.ServiceDate == serviceDate);

                    if (existing != null)
                    {
                        existing.IsPresent = record.IsPresent;
                        existing.IsFlagged = record.IsFlagged;
                        existing.Notes = record.Notes ?? string.Empty;
                        existing.RecordedBy = recordedById;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _dbContext.Attendance.Add(new Attendance
                        {
                            MemberId = record.MemberId,
                            ServiceDate = serviceDate,
                            IsPresent = record.IsPresent,
                            IsFlagged = record.IsFlagged,
                            Notes = record.Notes ?? string.Empty,
                            RecordedBy = recordedById,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await UpdateMemberFlagStatus(record.MemberId, record.IsFlagged, serviceDate, record.IsPresent);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Attendance recorded successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to record bulk attendance for date {ServiceDate}", serviceDate);
                return StatusCode(500, new { error = "Failed to record attendance", details = ex.Message });
            }
        }

        private async Task AutoFlagMembersForFollowUp(DateTime serviceDate)
        {
            try
            {
                var cutoff = serviceDate.AddDays(-21);

                var membersToFlag = await _dbContext.Attendance
                    .Where(a => a.ServiceDate >= cutoff && a.ServiceDate <= serviceDate)
                    .GroupBy(a => a.MemberId)
                    .Where(g => g
                        .OrderByDescending(a => a.ServiceDate)
                        .Take(3)
                        .All(a => !a.IsPresent))
                    .Select(g => g.Key)
                    .ToListAsync();

                if (membersToFlag.Any())
                {
                    var members = await _dbContext.Members
                        .Where(m => membersToFlag.Contains(m.Id) && m.IsActive)
                        .ToListAsync();

                    foreach (var m in members)
                    {
                        m.IsFlagged = true;
                        m.AbsentSince ??= serviceDate;
                        m.UpdatedAt = DateTime.UtcNow;
                    }

                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-flag members for follow-up");
            }
        }

        private async Task UpdateMemberFlagStatus(int memberId, bool isFlagged, DateTime serviceDate, bool isPresent)
        {
            try
            {
                var member = await _dbContext.Members.FirstOrDefaultAsync(m => m.Id == memberId && m.IsActive);
                if (member == null) return;

                member.IsFlagged = isFlagged;
                member.AbsentSince = isFlagged && member.AbsentSince == null ? serviceDate :
                                     isPresent ? null : member.AbsentSince;
                member.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update member flag status for member {MemberId}", memberId);
            }
        }

        [HttpGet("range")]
        public async Task<IActionResult> GetAttendanceByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int? householdId = null)
        {
            if (startDate > endDate)
                return BadRequest(new { error = "Start date cannot be after end date" });

            if ((endDate - startDate).TotalDays > 365)
                return BadRequest(new { error = "Date range cannot exceed 1 year" });

            try
            {
                var query = _dbContext.Attendance
                    .Include(a => a.Member)
                    .ThenInclude(m => m.Household)
                    .Where(a => a.ServiceDate >= startDate && a.ServiceDate <= endDate);

                if (householdId.HasValue && householdId > 0)
                    query = query.Where(a => a.Member.HouseholdId == householdId.Value);

                var attendance = await query
                    .OrderByDescending(a => a.ServiceDate)
                    .ThenBy(a => a.Member.FullName)
                    .Select(a => new
                    {
                        ServiceDate = a.ServiceDate.ToString("yyyy-MM-dd"),
                        a.Member.FirstName,
                        a.Member.LastName,
                        a.Member.FullName,
                        a.Member.Gender,
                        HouseholdName = a.Member.Household != null ? a.Member.Household.Name : null,
                        a.IsPresent,
                        a.IsFlagged,
                        a.Notes,
                        a.RecordedBy
                    })
                    .ToListAsync();

                return Ok(new
                {
                    startDate = startDate.ToString("yyyy-MM-dd"),
                    endDate = endDate.ToString("yyyy-MM-dd"),
                    householdId,
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