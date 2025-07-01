using SP.RBAC.API.Entities;

namespace SP.RBAC.API.DTOs;

public class EntityInstanceDto : BaseAuditableDto
{
    public Guid EntityDefinitionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public SyncStatus? SyncStatus { get; set; }
    public string RawData { get; set; } = "{}";
    public string EntityDefinitionName { get; set; } = string.Empty;
    public List<PropertyValueDto> PropertyValues { get; set; } = new();
}

public class CreateEntityInstanceDto
{
    public Guid EntityDefinitionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string RawData { get; set; } = "{}";
    public List<CreatePropertyValueDto> PropertyValues { get; set; } = new();
}

public class UpdateEntityInstanceDto
{
    public Guid EntityDefinitionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string RawData { get; set; } = "{}";
    public string? LastModifiedReason { get; set; }
    public List<UpdatePropertyValueDto> PropertyValues { get; set; } = new();
}
