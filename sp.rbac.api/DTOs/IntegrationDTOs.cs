using SP.RBAC.API.Entities;
using System.ComponentModel.DataAnnotations;

namespace SP.RBAC.API.DTOs;

/// <summary>
/// DTO for integration mapping data transfer
/// </summary>
public class IntegrationMappingDto
{
    public Guid Id { get; set; }
    public Guid IntegrationSystemId { get; set; }
    public string IntegrationSystemName { get; set; } = string.Empty;
    public Guid PropertyDefinitionId { get; set; }
    public string PropertyDefinitionName { get; set; } = string.Empty;
    public string ExternalFieldName { get; set; } = string.Empty;
    public string InternalPropertyName { get; set; } = string.Empty;
    public string? TransformationRules { get; set; }
    public string? ValidationRule { get; set; }
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Request model for creating integration mappings
/// </summary>
public class CreateIntegrationMappingRequest
{
    [Required]
    public Guid IntegrationSystemId { get; set; }
    
    [Required]
    public Guid PropertyDefinitionId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string ExternalFieldName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string InternalPropertyName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? TransformationRules { get; set; }
    
    [StringLength(1000)]
    public string? ValidationRule { get; set; }
    
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    
    [StringLength(500)]
    public string? DefaultValue { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for updating integration mappings
/// </summary>
public class UpdateIntegrationMappingRequest
{
    [Required]
    [StringLength(100)]
    public string ExternalFieldName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string InternalPropertyName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? TransformationRules { get; set; }
    
    [StringLength(1000)]
    public string? ValidationRule { get; set; }
    
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    
    [StringLength(500)]
    public string? DefaultValue { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    
    [StringLength(1000)]
    public string? ChangeReason { get; set; }
}

/// <summary>
/// DTO for system relationship data transfer
/// </summary>
public class SystemRelationshipDto
{
    public Guid Id { get; set; }
    public Guid SourceSystemId { get; set; }
    public string SourceSystemName { get; set; } = string.Empty;
    public Guid TargetSystemId { get; set; }
    public string TargetSystemName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DataFlowDirection { get; set; }
    public string? DataFormat { get; set; }
    public string? Frequency { get; set; }
    public string? BusinessJustification { get; set; }
    public bool IsActive { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
}

/// <summary>
/// Request model for creating system relationships
/// </summary>
public class CreateSystemRelationshipRequest
{
    [Required]
    public Guid SourceSystemId { get; set; }
    
    [Required]
    public Guid TargetSystemId { get; set; }
    
    [Required]
    public RelationshipType RelationshipType { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [StringLength(100)]
    public string? DataFlow { get; set; }
    
    [StringLength(100)]
    public string? IntegrationMethod { get; set; }
    
    [StringLength(100)]
    public string? Frequency { get; set; }
    
    [StringLength(2000)]
    public string? BusinessJustification { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>
/// DTO for integration document data transfer
/// </summary>
public class IntegrationDocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? FilePath { get; set; }
    public string? FileType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Tags { get; set; }
    public int Version { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsActive { get; set; }
    public Guid? IntegrationSystemId { get; set; }
    public string? IntegrationSystemName { get; set; }
    public Guid? SystemRelationshipId { get; set; }
    public Guid? EntityDefinitionId { get; set; }
    public string? EntityDefinitionName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Request model for creating integration documents
/// </summary>
public class CreateIntegrationDocumentRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public DocumentType DocumentType { get; set; }
    
    public string? Content { get; set; }
    
    [StringLength(500)]
    public string? FilePath { get; set; }
    
    [StringLength(20)]
    public string? FileType { get; set; }
    
    public long? FileSizeBytes { get; set; }
    
    [StringLength(500)]
    public string? Tags { get; set; }
    
    public bool IsTemplate { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    [Required]
    public Guid IntegrationSystemId { get; set; }
    
    public Guid? SystemRelationshipId { get; set; }
    public Guid? EntityDefinitionId { get; set; }
}

/// <summary>
/// Request model for updating integration documents
/// </summary>
public class UpdateIntegrationDocumentRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public string? Content { get; set; }
    
    [StringLength(500)]
    public string? FilePath { get; set; }
    
    [StringLength(500)]
    public string? Tags { get; set; }
    
    public bool IsTemplate { get; set; }
    public bool IsActive { get; set; }
    
    [StringLength(1000)]
    public string? VersionDescription { get; set; }
    
    [StringLength(2000)]
    public string? ChangesSummary { get; set; }
}

/// <summary>
/// Request model for searching integration mappings
/// </summary>
public class IntegrationMappingSearchRequest
{
    public Guid? IntegrationSystemId { get; set; }
    public Guid? PropertyDefinitionId { get; set; }
    public string? ExternalFieldName { get; set; }
    public string? InternalPropertyName { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsActive { get; set; }
    
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Request model for searching system relationships
/// </summary>
public class SystemRelationshipSearchRequest
{
    public Guid? SourceSystemId { get; set; }
    public Guid? TargetSystemId { get; set; }
    public RelationshipType? RelationshipType { get; set; }
    public string? DataFlowDirection { get; set; }
    public string? DataFormat { get; set; }
    public bool? IsActive { get; set; }
    
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Request model for searching integration documents
/// </summary>
public class IntegrationDocumentSearchRequest
{
    public string? Title { get; set; }
    public DocumentType? DocumentType { get; set; }
    public Guid? IntegrationSystemId { get; set; }
    public Guid? SystemRelationshipId { get; set; }
    public string? Tags { get; set; }
    public bool? IsTemplate { get; set; }
    public bool? IsActive { get; set; }
    
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Response model for integration architecture overview
/// </summary>
public class IntegrationArchitectureDto
{
    public List<IntegrationSystemSummaryDto> Systems { get; set; } = new();
    public List<SystemRelationshipDto> Relationships { get; set; } = new();
    public Dictionary<string, int> SystemTypeDistribution { get; set; } = new();
    public Dictionary<string, int> RelationshipTypeDistribution { get; set; } = new();
    public int TotalSystems { get; set; }
    public int TotalRelationships { get; set; }
    public int TotalDocuments { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response model for integration mapping statistics
/// </summary>
public class IntegrationMappingStatsDto
{
    public int TotalMappings { get; set; }
    public int ActiveMappings { get; set; }
    public int RequiredMappings { get; set; }
    public int UniqueMappings { get; set; }
    public Dictionary<string, int> MappingsBySystem { get; set; } = new();
    public Dictionary<string, int> MappingsByEntity { get; set; } = new();
    public List<string> MostMappedSystems { get; set; } = new();
    public List<string> MostMappedEntities { get; set; } = new();
}

/// <summary>
/// Paginated response wrapper
/// </summary>
public class PaginatedResponseDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Integration mapping statistics response
/// </summary>
public class IntegrationMappingStatisticsDto
{
    public int TotalMappings { get; set; }
    public int ActiveMappings { get; set; }
    public int InactiveMappings { get; set; }
    public int SystemsWithMappings { get; set; }
    public int PropertiesMapped { get; set; }
    public List<SystemMappingCountDto> MappingsBySystem { get; set; } = new();
}

/// <summary>
/// System mapping count DTO
/// </summary>
public class SystemMappingCountDto
{
    public Guid SystemId { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public int MappingCount { get; set; }
}

/// <summary>
/// Update system relationship request
/// </summary>
public class UpdateSystemRelationshipRequest
{
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [StringLength(100)]
    public string? DataFlow { get; set; }
    
    [StringLength(100)]
    public string? IntegrationMethod { get; set; }
    
    [StringLength(100)]
    public string? Frequency { get; set; }
}

/// <summary>
/// System relationships wrapper
/// </summary>
public class SystemRelationshipsDto
{
    public Guid SystemId { get; set; }
    public List<SystemRelationshipDto> OutgoingRelationships { get; set; } = new();
    public List<SystemRelationshipDto> IncomingRelationships { get; set; } = new();
}

/// <summary>
/// Integration system summary DTO
/// </summary>
public class IntegrationSystemSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Relationship statistics DTO
/// </summary>
public class RelationshipStatisticsDto
{
    public int TotalRelationships { get; set; }
    public int ActiveRelationships { get; set; }
    public int InactiveRelationships { get; set; }
    public List<RelationshipTypeCountDto> RelationshipsByType { get; set; } = new();
    public int SystemsWithRelationships { get; set; }
}

/// <summary>
/// Relationship type count DTO
/// </summary>
public class RelationshipTypeCountDto
{
    public RelationshipType RelationshipType { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Integration document history DTO
/// </summary>
public class IntegrationDocumentHistoryDto
{
    public Guid Id { get; set; }
    public Guid IntegrationDocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int VersionNumber { get; set; }
    public string? ChangeDescription { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ArchivedAt { get; set; }
}

/// <summary>
/// Document statistics DTO
/// </summary>
public class DocumentStatisticsDto
{
    public int TotalDocuments { get; set; }
    public int ActiveDocuments { get; set; }
    public int InactiveDocuments { get; set; }
    public List<DocumentTypeCountDto> DocumentsByType { get; set; } = new();
    public int SystemsWithDocuments { get; set; }
    public int RecentlyUpdatedDocuments { get; set; }
}

/// <summary>
/// Document type count DTO
/// </summary>
public class DocumentTypeCountDto
{
    public DocumentType DocumentType { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Document search request
/// </summary>
public class DocumentSearchRequest
{
    [StringLength(200)]
    public string? SearchTerm { get; set; }
    
    public List<DocumentType>? DocumentTypes { get; set; }
    public List<Guid>? SystemIds { get; set; }
    
    [Range(1, 100)]
    public int MaxResults { get; set; } = 50;
}
