using SP.RBAC.API.Entities;

namespace SP.RBAC.API.DTOs;

public class AccessRuleDto : BaseAuditableDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? IntegrationSystemId { get; set; }
    public TriggerType TriggerType { get; set; }
    public string TriggerCondition { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public string ActionConfiguration { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastExecuted { get; set; }
    public string? LastExecutionResult { get; set; }
    public string? IntegrationSystemName { get; set; }
}

public class CreateAccessRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? IntegrationSystemId { get; set; }
    public TriggerType TriggerType { get; set; } = TriggerType.Manual;
    public string TriggerCondition { get; set; } = string.Empty;
    public ActionType ActionType { get; set; } = ActionType.AssignRole;
    public string ActionConfiguration { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateAccessRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? IntegrationSystemId { get; set; }
    public TriggerType TriggerType { get; set; }
    public string TriggerCondition { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public string ActionConfiguration { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? LastModifiedReason { get; set; }
}
