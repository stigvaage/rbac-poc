using Microsoft.AspNetCore.Mvc;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;
using SP.RBAC.API.Services;

namespace SP.RBAC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get audit history for a specific entity
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetEntityHistory(
        string entityType,
        string entityId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogs = await _auditService.GetEntityHistoryAsync(
                entityType, entityId, pageNumber, pageSize, cancellationToken);

            var auditLogDtos = auditLogs.Select(MapToDto).ToList();
            
            return Ok(new PagedResult<AuditLogDto>
            {
                Items = auditLogDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = auditLogDtos.Count // TODO: Implement proper total count
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity audit history for {EntityType}/{EntityId}", entityType, entityId);
            return StatusCode(500, "An error occurred while retrieving audit history");
        }
    }

    /// <summary>
    /// Get audit activity for a specific user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetUserActivity(
        string userId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogs = await _auditService.GetUserActivityAsync(
                userId, fromDate, toDate, pageNumber, pageSize, cancellationToken);

            var auditLogDtos = auditLogs.Select(MapToDto).ToList();
            
            return Ok(new PagedResult<AuditLogDto>
            {
                Items = auditLogDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = auditLogDtos.Count // TODO: Implement proper total count
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user audit activity for {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user activity");
        }
    }

    /// <summary>
    /// Search audit logs with advanced filtering
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Search(
        [FromBody] AuditSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogs = await _auditService.SearchAsync(
                request.EntityType,
                request.Action,
                request.UserId,
                request.FromDate,
                request.ToDate,
                request.CorrelationId,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var auditLogDtos = auditLogs.Select(MapToDto).ToList();
            
            return Ok(new PagedResult<AuditLogDto>
            {
                Items = auditLogDtos,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = auditLogDtos.Count // TODO: Implement proper total count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit logs");
            return StatusCode(500, "An error occurred while searching audit logs");
        }
    }

    /// <summary>
    /// Get compliance report summary
    /// </summary>
    [HttpGet("compliance-report")]
    public async Task<ActionResult<ComplianceReportDto>> GetComplianceReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var endDate = toDate ?? DateTime.UtcNow;

            var auditLogs = await _auditService.SearchAsync(
                fromDate: startDate,
                toDate: endDate,
                pageSize: int.MaxValue,
                cancellationToken: cancellationToken);

            var report = new ComplianceReportDto
            {
                ReportPeriodStart = startDate,
                ReportPeriodEnd = endDate,
                TotalActivities = auditLogs.Count(),
                ActivitiesByAction = auditLogs
                    .GroupBy(a => a.Action)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ActivitiesByEntityType = auditLogs
                    .GroupBy(a => a.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                UniqueUsers = auditLogs.Select(a => a.UserId).Distinct().Count(),
                MostActiveUsers = auditLogs
                    .GroupBy(a => new { a.UserId, a.UserName })
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key.UserName ?? g.Key.UserId, g => g.Count())
            };

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report");
            return StatusCode(500, "An error occurred while generating compliance report");
        }
    }

    /// <summary>
    /// Get activity summary for dashboard
    /// </summary>
    [HttpGet("activity-summary")]
    public async Task<ActionResult<ActivitySummaryDto>> GetActivitySummary(
        [FromQuery] int hours = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddHours(-hours);
            
            var auditLogs = await _auditService.SearchAsync(
                fromDate: fromDate,
                pageSize: int.MaxValue,
                cancellationToken: cancellationToken);

            var summary = new ActivitySummaryDto
            {
                PeriodHours = hours,
                TotalActivities = auditLogs.Count(),
                RecentActivities = auditLogs
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .Select(MapToDto)
                    .ToList(),
                HourlyBreakdown = auditLogs
                    .GroupBy(a => a.CreatedAt.Hour)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating activity summary");
            return StatusCode(500, "An error occurred while generating activity summary");
        }
    }

    private static AuditLogDto MapToDto(AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            Action = auditLog.Action.ToString(),
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            UserId = auditLog.UserId,
            UserName = auditLog.UserName,
            Justification = auditLog.Justification,
            CorrelationId = auditLog.CorrelationId,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            RequestPath = auditLog.RequestPath,
            RequestMethod = auditLog.RequestMethod,
            ResponseStatusCode = auditLog.ResponseStatusCode,
            ExecutionTimeMs = auditLog.ExecutionTimeMs,
            CreatedAt = auditLog.CreatedAt,
            CreatedBy = auditLog.CreatedBy ?? string.Empty
        };
    }
}
