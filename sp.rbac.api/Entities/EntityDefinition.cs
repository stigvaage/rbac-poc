namespace SP.RBAC.API.Entities;

/// <summary>
/// Defines the structure of entities within an integration system (e.g., User, Role, Department)
/// </summary>
public class EntityDefinition : BaseAuditableEntity
{
    public Guid IntegrationSystemId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "User", "Role", "Department"
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty; // Source table/endpoint name
    public string PrimaryKeyField { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public string Metadata { get; set; } = "{}"; // JSON string
    
    // Navigation properties
    public virtual IntegrationSystem IntegrationSystem { get; set; } = null!;
    public virtual ICollection<PropertyDefinition> PropertyDefinitions { get; set; } = new List<PropertyDefinition>();
    public virtual ICollection<EntityInstance> EntityInstances { get; set; } = new List<EntityInstance>();
}
