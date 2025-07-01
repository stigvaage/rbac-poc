using SP.RBAC.API.Entities;

namespace SP.RBAC.API.DTOs;

public class SyncLogDto : BaseDto
{
    public Guid IntegrationSystemId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public SyncStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public string? ErrorMessage { get; set; }
    public string Details { get; set; } = "{}";
    public string IntegrationSystemName { get; set; } = string.Empty;
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    public decimal SuccessRate => TotalRecords > 0 ? (decimal)SuccessfulRecords / TotalRecords * 100 : 0;
}

public class CreateSyncLogDto
{
    public Guid IntegrationSystemId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public int TotalRecords { get; set; } = 0;
    public string Details { get; set; } = "{}";
}

public class UpdateSyncLogDto
{
    public SyncStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public string? ErrorMessage { get; set; }
    public string Details { get; set; } = "{}";
}

public class SyncLogSummaryDto
{
    public Guid IntegrationSystemId { get; set; }
    public string IntegrationSystemName { get; set; } = string.Empty;
    public int TotalSyncs { get; set; }
    public int SuccessfulSyncs { get; set; }
    public int FailedSyncs { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public SyncStatus? LastSyncStatus { get; set; }
    public decimal SuccessRate => TotalSyncs > 0 ? (decimal)SuccessfulSyncs / TotalSyncs * 100 : 0;
}
