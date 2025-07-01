namespace SP.RBAC.API.Entities;

/// <summary>
/// Represents a specific instance of an entity (e.g., a specific user, role, department)
/// </summary>
public class EntityInstance : BaseAuditableEntity
{
    public Guid EntityDefinitionId { get; set; }
    public string ExternalId { get; set; } = string.Empty; // ID from external system
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncedAt { get; set; }
    public SyncStatus? SyncStatus { get; set; }
    public string RawData { get; set; } = "{}"; // JSON string - Original data from external system
    
    // Navigation properties
    public virtual EntityDefinition EntityDefinition { get; set; } = null!;
    public virtual ICollection<PropertyValue> PropertyValues { get; set; } = new List<PropertyValue>();
    public virtual ICollection<AccessAssignment> UserAccessAssignments { get; set; } = new List<AccessAssignment>();
    public virtual ICollection<AccessAssignment> RoleAccessAssignments { get; set; } = new List<AccessAssignment>();
}
