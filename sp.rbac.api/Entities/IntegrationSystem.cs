namespace SP.RBAC.API.Entities;

/// <summary>
/// Represents an external system that can be integrated with the IAM solution
/// </summary>
public class IntegrationSystem : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty; // e.g., "HR", "EMR", "Financial"
    public string SystemVersion { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.Database;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSync { get; set; }
    public SyncStatus? LastSyncStatus { get; set; }
    public string Configuration { get; set; } = "{}"; // JSON string
    
    // Navigation properties
    public virtual ICollection<EntityDefinition> EntityDefinitions { get; set; } = new List<EntityDefinition>();
    public virtual ICollection<SyncLog> SyncLogs { get; set; } = new List<SyncLog>();
    public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
    public virtual ICollection<AccessAssignment> AccessAssignments { get; set; } = new List<AccessAssignment>();
}
