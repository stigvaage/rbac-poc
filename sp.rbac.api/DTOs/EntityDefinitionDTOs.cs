using SP.RBAC.API.Entities;

namespace SP.RBAC.API.DTOs;

public class EntityDefinitionDto : BaseAuditableDto
{
    public Guid IntegrationSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string PrimaryKeyField { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string Metadata { get; set; } = "{}";
    public string IntegrationSystemName { get; set; } = string.Empty;
    public int PropertyDefinitionsCount { get; set; }
    public int EntityInstancesCount { get; set; }
}

public class CreateEntityDefinitionDto
{
    public Guid IntegrationSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string PrimaryKeyField { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public string Metadata { get; set; } = "{}";
}

public class UpdateEntityDefinitionDto
{
    public Guid IntegrationSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string PrimaryKeyField { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string Metadata { get; set; } = "{}";
    public string? LastModifiedReason { get; set; }
}
