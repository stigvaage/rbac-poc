using SP.RBAC.API.Entities;

namespace SP.RBAC.API.DTOs;

public class PropertyValueDto : BaseAuditableDto
{
    public Guid EntityInstanceId { get; set; }
    public Guid PropertyDefinitionId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? DisplayValue { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    
    // Navigation property display names
    public string EntityInstanceDisplayName { get; set; } = string.Empty;
    public string PropertyDefinitionName { get; set; } = string.Empty;
    public DataType PropertyDataType { get; set; }
    public string EntityDefinitionName { get; set; } = string.Empty;
}

public class CreatePropertyValueDto
{
    public Guid EntityInstanceId { get; set; }
    public Guid PropertyDefinitionId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? DisplayValue { get; set; }
    public bool IsDefault { get; set; } = false;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class UpdatePropertyValueDto
{
    public Guid? Id { get; set; } // Null for new values
    public Guid EntityInstanceId { get; set; }
    public Guid PropertyDefinitionId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? DisplayValue { get; set; }
    public bool IsDefault { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? LastModifiedReason { get; set; }
}

public class PropertyValueHistoryDto
{
    public Guid PropertyDefinitionId { get; set; }
    public string PropertyDefinitionName { get; set; } = string.Empty;
    public List<PropertyValueDto> ValueHistory { get; set; } = new();
}
