namespace SP.RBAC.API.Entities;

/// <summary>
/// Represents a mapping between external system fields and internal entity properties
/// Used for documenting and managing data transformations during system integrations
/// </summary>
public sealed class IntegrationMapping : BaseAuditableEntity
{
    public Guid IntegrationSystemId { get; set; }
    public IntegrationSystem IntegrationSystem { get; set; } = null!;

    public Guid PropertyDefinitionId { get; set; }
    public PropertyDefinition PropertyDefinition { get; set; } = null!;

    public string ExternalFieldName { get; set; } = string.Empty;
    public string InternalPropertyName { get; set; } = string.Empty;
    public string? TransformationRules { get; set; }
    public string? ValidationRule { get; set; }
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<IntegrationMappingHistory> MappingHistories { get; set; } = new List<IntegrationMappingHistory>();

    // Private constructor for DDD pattern
    private IntegrationMapping() { }

    /// <summary>
    /// Factory method to create a new integration mapping
    /// </summary>
    public static IntegrationMapping Create(
        Guid integrationSystemId,
        Guid propertyDefinitionId,
        string externalFieldName,
        string internalPropertyName,
        string? transformationRules = null)
    {
        if (integrationSystemId == Guid.Empty) throw new ArgumentException("Integration system ID cannot be empty", nameof(integrationSystemId));
        if (propertyDefinitionId == Guid.Empty) throw new ArgumentException("Property definition ID cannot be empty", nameof(propertyDefinitionId));
        if (string.IsNullOrWhiteSpace(externalFieldName)) throw new ArgumentException("External field name cannot be empty", nameof(externalFieldName));
        if (string.IsNullOrWhiteSpace(internalPropertyName)) throw new ArgumentException("Internal property name cannot be empty", nameof(internalPropertyName));

        return new IntegrationMapping
        {
            Id = Guid.NewGuid(),
            IntegrationSystemId = integrationSystemId,
            PropertyDefinitionId = propertyDefinitionId,
            ExternalFieldName = externalFieldName.Trim(),
            InternalPropertyName = internalPropertyName.Trim(),
            TransformationRules = transformationRules?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // TODO: Get from current user context
        };
    }

    public void UpdateInternalPropertyName(string internalPropertyName)
    {
        if (string.IsNullOrWhiteSpace(internalPropertyName)) throw new ArgumentException("Internal property name cannot be empty", nameof(internalPropertyName));
        InternalPropertyName = internalPropertyName.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void UpdateTransformationRules(string? transformationRules)
    {
        TransformationRules = transformationRules?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void SetDefaultValue(string? defaultValue)
    {
        DefaultValue = defaultValue?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }
}

/// <summary>
/// Tracks changes to integration mappings over time
/// Essential for maintaining audit trail of integration configuration changes
/// </summary>
public sealed class IntegrationMappingHistory : BaseEntity
{
    public Guid IntegrationMappingId { get; set; }
    public IntegrationMapping IntegrationMapping { get; set; } = null!;

    public string ChangeType { get; set; } = string.Empty; // Created, Modified, Deleted, Activated, Deactivated
    public string? OldConfiguration { get; set; } // JSON of previous state
    public string? NewConfiguration { get; set; } // JSON of new state
    public string? ChangeReason { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? ApprovalStatus { get; set; } // Pending, Approved, Rejected
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Private constructor for DDD pattern
    private IntegrationMappingHistory() { }

    /// <summary>
    /// Factory method to create a new mapping history entry
    /// </summary>
    public static IntegrationMappingHistory Create(
        Guid integrationMappingId,
        string changeType,
        string? oldConfiguration = null,
        string? newConfiguration = null,
        string? changeReason = null)
    {
        if (integrationMappingId == Guid.Empty) throw new ArgumentException("Integration mapping ID cannot be empty", nameof(integrationMappingId));
        if (string.IsNullOrWhiteSpace(changeType)) throw new ArgumentException("Change type cannot be empty", nameof(changeType));

        return new IntegrationMappingHistory
        {
            Id = Guid.NewGuid(),
            IntegrationMappingId = integrationMappingId,
            ChangeType = changeType.Trim(),
            OldConfiguration = oldConfiguration?.Trim(),
            NewConfiguration = newConfiguration?.Trim(),
            ChangeReason = changeReason?.Trim(),
            ChangedBy = "System", // TODO: Get from current user context
            ChangedAt = DateTime.UtcNow,
            ApprovalStatus = "Approved" // TODO: Implement approval workflow
        };
    }
}

/// <summary>
/// Documents the relationship and dependencies between different integration systems
/// Critical for understanding system architecture and impact analysis
/// </summary>
public sealed class SystemRelationship : BaseAuditableEntity
{
    public Guid SourceSystemId { get; set; }
    public IntegrationSystem SourceSystem { get; set; } = null!;

    public Guid TargetSystemId { get; set; }
    public IntegrationSystem TargetSystem { get; set; } = null!;

    public RelationshipType RelationshipType { get; set; }
    public string? Description { get; set; }
    public string? DataFlow { get; set; } // Bidirectional, SourceToTarget, TargetToSource
    public string? IntegrationMethod { get; set; } // API, Database, File, Message Queue
    public string? Frequency { get; set; } // Real-time, Hourly, Daily, Weekly, Monthly, On-demand
    public string? BusinessJustification { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    // Navigation properties
    public ICollection<IntegrationDocument> RelatedDocuments { get; set; } = new List<IntegrationDocument>();

    // Private constructor for DDD pattern
    private SystemRelationship() { }

    /// <summary>
    /// Factory method to create a new system relationship
    /// </summary>
    public static SystemRelationship Create(
        Guid sourceSystemId,
        Guid targetSystemId,
        RelationshipType relationshipType,
        string? description = null)
    {
        if (sourceSystemId == Guid.Empty) throw new ArgumentException("Source system ID cannot be empty", nameof(sourceSystemId));
        if (targetSystemId == Guid.Empty) throw new ArgumentException("Target system ID cannot be empty", nameof(targetSystemId));
        if (sourceSystemId == targetSystemId) throw new ArgumentException("Source and target systems cannot be the same", nameof(targetSystemId));

        return new SystemRelationship
        {
            Id = Guid.NewGuid(),
            SourceSystemId = sourceSystemId,
            TargetSystemId = targetSystemId,
            RelationshipType = relationshipType,
            Description = description?.Trim(),
            IsActive = true,
            EffectiveDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // TODO: Get from current user context
        };
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void SetDataFlow(string? dataFlow)
    {
        DataFlow = dataFlow?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void SetIntegrationMethod(string? integrationMethod)
    {
        IntegrationMethod = integrationMethod?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void SetFrequency(string? frequency)
    {
        Frequency = frequency?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }
}

/// <summary>
/// Stores documentation, diagrams, and procedural information for system integrations
/// Serves as a knowledge base for integration management and troubleshooting
/// </summary>
public sealed class IntegrationDocument : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DocumentType DocumentType { get; set; }
    public string? Content { get; set; } // Can store markdown, HTML, or other formats
    public string? FilePath { get; set; } // Path to external file if content is stored separately
    public string? FileType { get; set; } // .md, .html, .pdf, .docx, .jpg, .png, etc.
    public long? FileSizeBytes { get; set; }
    public string? Tags { get; set; } // Comma-separated tags for categorization
    public new int Version { get; set; } = 1; // Document version (overrides BaseAuditableEntity.Version)
    public bool IsTemplate { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Optional associations
    public Guid IntegrationSystemId { get; set; }
    public IntegrationSystem? IntegrationSystem { get; set; }

    public Guid? SystemRelationshipId { get; set; }
    public SystemRelationship? SystemRelationship { get; set; }

    public Guid? EntityDefinitionId { get; set; }
    public EntityDefinition? EntityDefinition { get; set; }

    // Navigation properties
    public ICollection<IntegrationDocumentHistory> DocumentHistories { get; set; } = new List<IntegrationDocumentHistory>();

    // Private constructor for DDD pattern
    private IntegrationDocument() { }

    /// <summary>
    /// Factory method to create a new integration document
    /// </summary>
    public static IntegrationDocument Create(
        Guid integrationSystemId,
        string title,
        DocumentType documentType,
        string? content = null)
    {
        if (integrationSystemId == Guid.Empty) throw new ArgumentException("Integration system ID cannot be empty", nameof(integrationSystemId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty", nameof(title));

        return new IntegrationDocument
        {
            Id = Guid.NewGuid(),
            IntegrationSystemId = integrationSystemId,
            Title = title.Trim(),
            DocumentType = documentType,
            Content = content?.Trim(),
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // TODO: Get from current user context
        };
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty", nameof(title));
        Title = title.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void UpdateContent(string? content)
    {
        Content = content?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void SetFilePath(string? filePath)
    {
        FilePath = filePath?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void SetTags(string? tags)
    {
        Tags = tags?.Trim();
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void AssignToRelationship(Guid systemRelationshipId)
    {
        if (systemRelationshipId == Guid.Empty) throw new ArgumentException("System relationship ID cannot be empty", nameof(systemRelationshipId));
        SystemRelationshipId = systemRelationshipId;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "System"; // TODO: Get from current user context
    }
}

/// <summary>
/// Maintains version history of integration documents
/// Enables rollback capabilities and change tracking for documentation
/// </summary>
public sealed class IntegrationDocumentHistory : BaseEntity
{
    public Guid IntegrationDocumentId { get; set; }
    public IntegrationDocument IntegrationDocument { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int VersionNumber { get; set; }
    public string? ChangeDescription { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    // Private constructor for DDD pattern
    private IntegrationDocumentHistory() { }

    /// <summary>
    /// Factory method to create a new document history entry
    /// </summary>
    public static IntegrationDocumentHistory Create(
        Guid integrationDocumentId,
        string title,
        string? content,
        int versionNumber,
        string? changeDescription = null)
    {
        if (integrationDocumentId == Guid.Empty) throw new ArgumentException("Integration document ID cannot be empty", nameof(integrationDocumentId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty", nameof(title));

        return new IntegrationDocumentHistory
        {
            Id = Guid.NewGuid(),
            IntegrationDocumentId = integrationDocumentId,
            Title = title.Trim(),
            Content = content?.Trim(),
            VersionNumber = versionNumber,
            ChangeDescription = changeDescription?.Trim(),
            ChangedBy = "System", // TODO: Get from current user context
            ArchivedAt = DateTime.UtcNow
        };
    }
}
