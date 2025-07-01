namespace SP.RBAC.API.Entities;

/// <summary>
/// Logs synchronization activities with external systems
/// </summary>
public class SyncLog : BaseEntity
{
    public Guid IntegrationSystemId { get; set; }
    public string Operation { get; set; } = string.Empty; // "Import", "Export", "Update", "Delete"
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TotalRecords { get; set; } = 0;
    public int ProcessedRecords { get; set; } = 0;
    public int SuccessfulRecords { get; set; } = 0;
    public int FailedRecords { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public string Details { get; set; } = "{}"; // JSON string
    
    // Navigation properties
    public virtual IntegrationSystem IntegrationSystem { get; set; } = null!;
}
