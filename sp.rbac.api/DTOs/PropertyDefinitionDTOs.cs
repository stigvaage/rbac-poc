using SP.RBAC.API.Entities;

namespace SP.RBAC.API.DTOs;

public class PropertyDefinitionDto : BaseAuditableDto
{
    public Guid EntityDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DataType DataType { get; set; }
    public string SourceField { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsDisplayed { get; set; }
    public bool IsEditable { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string UIMetadata { get; set; } = "{}";
    public string EntityDefinitionName { get; set; } = string.Empty;
}

public class CreatePropertyDefinitionDto
{
    public Guid EntityDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DataType DataType { get; set; } = DataType.String;
    public string SourceField { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public bool IsUnique { get; set; } = false;
    public bool IsSearchable { get; set; } = true;
    public bool IsDisplayed { get; set; } = true;
    public bool IsEditable { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string UIMetadata { get; set; } = "{}";
}

public class UpdatePropertyDefinitionDto
{
    public Guid EntityDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DataType DataType { get; set; }
    public string SourceField { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsDisplayed { get; set; }
    public bool IsEditable { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string UIMetadata { get; set; } = "{}";
    public string? LastModifiedReason { get; set; }
}
