using SP.RBAC.API.Entities;

namespace SP.RBAC.API.DTOs;

public class IntegrationSystemDto : BaseAuditableDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public string SystemVersion { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public AuthenticationType AuthenticationType { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSync { get; set; }
    public SyncStatus? LastSyncStatus { get; set; }
    public string Configuration { get; set; } = "{}";
}

public class CreateIntegrationSystemDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public string SystemVersion { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.Database;
    public bool IsActive { get; set; } = true;
    public string Configuration { get; set; } = "{}";
}

public class UpdateIntegrationSystemDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public string SystemVersion { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public AuthenticationType AuthenticationType { get; set; }
    public bool IsActive { get; set; }
    public string Configuration { get; set; } = "{}";
    public string? LastModifiedReason { get; set; }
}
