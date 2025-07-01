namespace SP.RBAC.API.Entities;

/// <summary>
/// Defines business rules for automatic access assignment
/// </summary>
public class AccessRule : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? IntegrationSystemId { get; set; }
    public TriggerType TriggerType { get; set; } = TriggerType.Manual;
    public string TriggerCondition { get; set; } = string.Empty; // JSON expression
    public ActionType ActionType { get; set; } = ActionType.AssignRole;
    public string ActionConfiguration { get; set; } = string.Empty; // JSON configuration
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime? LastExecuted { get; set; }
    public string? LastExecutionResult { get; set; }
    
    // Navigation properties
    public virtual IntegrationSystem? IntegrationSystem { get; set; }
    public virtual ICollection<AccessAssignment> AccessAssignments { get; set; } = new List<AccessAssignment>();
}
