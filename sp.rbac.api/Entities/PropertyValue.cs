namespace SP.RBAC.API.Entities;

/// <summary>
/// Stores the actual values for entity properties (EAV pattern)
/// </summary>
public class PropertyValue : BaseAuditableEntity
{
    public Guid EntityInstanceId { get; set; }
    public Guid PropertyDefinitionId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? DisplayValue { get; set; } // Formatted/processed value for display
    public bool IsDefault { get; set; } = false;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    
    // Navigation properties
    public virtual EntityInstance EntityInstance { get; set; } = null!;
    public virtual PropertyDefinition PropertyDefinition { get; set; } = null!;
}
