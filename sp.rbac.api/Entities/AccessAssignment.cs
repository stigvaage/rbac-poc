namespace SP.RBAC.API.Entities;

/// <summary>
/// Represents access assignments (user-role-system mappings)
/// </summary>
public class AccessAssignment : BaseAuditableEntity
{
    public Guid UserId { get; set; } // EntityInstance representing a user
    public Guid RoleId { get; set; } // EntityInstance representing a role
    public Guid TargetSystemId { get; set; } // IntegrationSystem where access is granted
    public AssignmentType AssignmentType { get; set; } = AssignmentType.Direct;
    public string? AssignmentReason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Metadata { get; set; } = "{}"; // JSON string
    
    // Navigation properties
    public virtual EntityInstance User { get; set; } = null!;
    public virtual EntityInstance Role { get; set; } = null!;
    public virtual IntegrationSystem TargetSystem { get; set; } = null!;
    public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
}
