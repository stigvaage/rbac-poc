namespace SP.RBAC.API.Entities;

/// <summary>
/// Represents an external system that can be integrated with the IAM solution
/// </summary>
public class IntegrationSystem : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty; // e.g., "HR", "EMR", "Financial"
    public string SystemVersion { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance, etc.
    public string ConnectionString { get; set; } = string.Empty;
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.Database;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSync { get; set; }
    public SyncStatus? LastSyncStatus { get; set; }
    public string Configuration { get; set; } = "{}"; // JSON string
    
    // Extended properties for documentation and integration management
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? BusinessOwner { get; set; }
    public string? TechnicalOwner { get; set; }
    public string? Environment { get; set; } // Development, Test, Production
    public string? Location { get; set; } // Physical or cloud location
    public DateTime? GoLiveDate { get; set; }
    public DateTime? MaintenanceWindow { get; set; }
    public string? SlaRequirements { get; set; }
    public string? SecurityClassification { get; set; } // Public, Internal, Confidential, Restricted
    public string? ComplianceRequirements { get; set; } // GDPR, HIPAA, SOX, etc.
    public string? Tags { get; set; } // Comma-separated tags for categorization
    
    // Navigation properties
    public virtual ICollection<EntityDefinition> EntityDefinitions { get; set; } = new List<EntityDefinition>();
    public virtual ICollection<SyncLog> SyncLogs { get; set; } = new List<SyncLog>();
    public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
    public virtual ICollection<AccessAssignment> AccessAssignments { get; set; } = new List<AccessAssignment>();
    
    // New navigation properties for integration documentation
    public virtual ICollection<IntegrationMapping> IntegrationMappings { get; set; } = new List<IntegrationMapping>();
    public virtual ICollection<SystemRelationship> SourceRelationships { get; set; } = new List<SystemRelationship>();
    public virtual ICollection<SystemRelationship> TargetRelationships { get; set; } = new List<SystemRelationship>();
    public virtual ICollection<IntegrationDocument> Documents { get; set; } = new List<IntegrationDocument>();
}
