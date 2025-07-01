namespace SP.RBAC.API.Entities;

/// <summary>
/// Defines properties/attributes for entities (e.g., User.Username, User.Email, Role.Name)
/// </summary>
public class PropertyDefinition : BaseAuditableEntity
{
    public Guid EntityDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Username", "Email", "RoleName"
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DataType DataType { get; set; } = DataType.String;
    public string SourceField { get; set; } = string.Empty; // Field name in source system
    public bool IsRequired { get; set; } = false;
    public bool IsUnique { get; set; } = false;
    public bool IsSearchable { get; set; } = true;
    public bool IsDisplayed { get; set; } = true;
    public bool IsEditable { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; } // JSON string with validation rules
    public string UIMetadata { get; set; } = "{}"; // JSON string for dynamic UI generation
    
    // Navigation properties
    public virtual EntityDefinition EntityDefinition { get; set; } = null!;
    public virtual ICollection<PropertyValue> PropertyValues { get; set; } = new List<PropertyValue>();
}
