using SP.RBAC.API.Entities;

namespace SP.RBAC.API.DTOs;

public class AccessAssignmentDto : BaseAuditableDto
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid TargetSystemId { get; set; }
    public AssignmentType AssignmentType { get; set; }
    public string? AssignmentReason { get; set; }
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Metadata { get; set; } = "{}";
    
    // Navigation property display names
    public string UserDisplayName { get; set; } = string.Empty;
    public string RoleDisplayName { get; set; } = string.Empty;
    public string TargetSystemName { get; set; } = string.Empty;
}

public class CreateAccessAssignmentDto
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid TargetSystemId { get; set; }
    public AssignmentType AssignmentType { get; set; } = AssignmentType.Direct;
    public string? AssignmentReason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Metadata { get; set; } = "{}";
}

public class UpdateAccessAssignmentDto
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid TargetSystemId { get; set; }
    public AssignmentType AssignmentType { get; set; }
    public string? AssignmentReason { get; set; }
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Metadata { get; set; } = "{}";
    public string? LastModifiedReason { get; set; }
}
