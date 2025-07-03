using SP.RBAC.API.Entities;
using System.ComponentModel.DataAnnotations;

namespace SP.RBAC.API.DTOs;

/// <summary>
/// DTO for audit log data transfer
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Justification { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? ResponseStatusCode { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Request model for audit search operations
/// </summary>
public class AuditSearchRequest
{
    public string? EntityType { get; set; }
    public AuditAction? Action { get; set; }
    public string? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? CorrelationId { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; set; } = 1;
    
    [Range(1, 1000, ErrorMessage = "Page size must be between 1 and 1000")]
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// DTO for compliance reporting
/// </summary>
public class ComplianceReportDto
{
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
    public int TotalActivities { get; set; }
    public Dictionary<string, int> ActivitiesByAction { get; set; } = new();
    public Dictionary<string, int> ActivitiesByEntityType { get; set; } = new();
    public int UniqueUsers { get; set; }
    public Dictionary<string, int> MostActiveUsers { get; set; } = new();
}

/// <summary>
/// DTO for activity summary dashboard
/// </summary>
public class ActivitySummaryDto
{
    public int PeriodHours { get; set; }
    public int TotalActivities { get; set; }
    public List<AuditLogDto> RecentActivities { get; set; } = new();
    public Dictionary<int, int> HourlyBreakdown { get; set; } = new();
}

/// <summary>
/// Request model for creating audit log entries
/// </summary>
public class CreateAuditLogRequest
{
    [Required]
    public string EntityType { get; set; } = string.Empty;
    
    [Required]
    public string EntityId { get; set; } = string.Empty;
    
    [Required]
    public AuditAction Action { get; set; }
    
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public string? UserName { get; set; }
    public string? Justification { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public int? ResponseStatusCode { get; set; }
    public long? ExecutionTimeMs { get; set; }
}

/// <summary>
/// Response model for audit operation results
/// </summary>
public class AuditOperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? AuditLogId { get; set; }
    public DateTime? Timestamp { get; set; }
}

/// <summary>
/// DTO for audit log statistics
/// </summary>
public class AuditStatisticsDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalEntries { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
    public Dictionary<string, int> UserCounts { get; set; } = new();
    public Dictionary<string, int> DailyCounts { get; set; } = new();
    public double AverageExecutionTimeMs { get; set; }
    public List<string> TopEntityTypes { get; set; } = new();
    public List<string> TopUsers { get; set; } = new();
}

/// <summary>
/// Request model for bulk audit operations
/// </summary>
public class BulkAuditRequest
{
    [Required]
    public List<CreateAuditLogRequest> AuditEntries { get; set; } = new();
    
    public string? BatchCorrelationId { get; set; }
    public string? BatchDescription { get; set; }
}

/// <summary>
/// Response model for bulk audit operations
/// </summary>
public class BulkAuditResponse
{
    public int TotalRequested { get; set; }
    public int SuccessfullyProcessed { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? BatchCorrelationId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public long ProcessingTimeMs { get; set; }
}
