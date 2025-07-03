using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;
using AutoMapper;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Controller for managing integration documentation and diagrams
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class IntegrationDocumentController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<IntegrationDocumentController> _logger;

    public IntegrationDocumentController(
        RbacDbContext context, 
        IMapper mapper, 
        ILogger<IntegrationDocumentController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all integration documents with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponseDto<IntegrationDocumentDto>>> GetIntegrationDocuments(
        [FromQuery] IntegrationDocumentSearchRequest request)
    {
        var query = _context.IntegrationDocuments
            .Include(d => d.IntegrationSystem)
            .Include(d => d.SystemRelationship)
            .AsQueryable();

        // Apply filters
        if (request.IntegrationSystemId.HasValue)
        {
            query = query.Where(d => d.IntegrationSystemId == request.IntegrationSystemId.Value);
        }

        if (request.DocumentType.HasValue)
        {
            query = query.Where(d => d.DocumentType == request.DocumentType.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            query = query.Where(d => EF.Functions.Like(d.Title, $"%{request.Title}%"));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == request.IsActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var documents = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var documentDtos = _mapper.Map<List<IntegrationDocumentDto>>(documents);

        var response = new PaginatedResponseDto<IntegrationDocumentDto>
        {
            Items = documentDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        _logger.LogInformation("Retrieved {Count} integration documents for page {Page}", documents.Count, request.Page);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific integration document by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IntegrationDocumentDto>> GetIntegrationDocument(Guid id)
    {
        var document = await _context.IntegrationDocuments
            .Include(d => d.IntegrationSystem)
            .Include(d => d.SystemRelationship)
            .Include(d => d.DocumentHistories)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            _logger.LogWarning("Integration document with ID {Id} not found", id);
            return NotFound($"Integration document with ID {id} not found");
        }

        var documentDto = _mapper.Map<IntegrationDocumentDto>(document);
        return Ok(documentDto);
    }

    /// <summary>
    /// Creates a new integration document
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<IntegrationDocumentDto>> CreateIntegrationDocument(
        CreateIntegrationDocumentRequest request)
    {
        // Validate integration system exists
        var integrationSystemExists = await _context.IntegrationSystems
            .AnyAsync(s => s.Id == request.IntegrationSystemId);
        if (!integrationSystemExists)
        {
            return BadRequest($"Integration system with ID {request.IntegrationSystemId} not found");
        }

        // Validate system relationship exists if provided
        if (request.SystemRelationshipId.HasValue)
        {
            var relationshipExists = await _context.SystemRelationships
                .AnyAsync(r => r.Id == request.SystemRelationshipId.Value);
            if (!relationshipExists)
            {
                return BadRequest($"System relationship with ID {request.SystemRelationshipId.Value} not found");
            }
        }

        var document = IntegrationDocument.Create(
            request.IntegrationSystemId,
            request.Title,
            request.DocumentType,
            request.Content);

        if (request.SystemRelationshipId.HasValue)
        {
            document.AssignToRelationship(request.SystemRelationshipId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            document.UpdateDescription(request.Description);
        }

        if (!string.IsNullOrWhiteSpace(request.FilePath))
        {
            document.SetFilePath(request.FilePath);
        }

        if (!string.IsNullOrWhiteSpace(request.Tags))
        {
            document.SetTags(request.Tags);
        }

        _context.IntegrationDocuments.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created integration document {Id} titled '{Title}'", 
            document.Id, request.Title);

        var documentDto = _mapper.Map<IntegrationDocumentDto>(document);
        return CreatedAtAction(nameof(GetIntegrationDocument), new { id = document.Id }, documentDto);
    }

    /// <summary>
    /// Updates an existing integration document
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IntegrationDocumentDto>> UpdateIntegrationDocument(
        Guid id, 
        UpdateIntegrationDocumentRequest request)
    {
        var document = await _context.IntegrationDocuments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return NotFound($"Integration document with ID {id} not found");
        }

        // Create history entry before updating
        var historyEntry = IntegrationDocumentHistory.Create(
            document.Id,
            document.Title,
            document.Content,
            document.Version,
            $"Updated via API at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        _context.IntegrationDocumentHistories.Add(historyEntry);

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            document.UpdateTitle(request.Title);
        }

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            document.UpdateContent(request.Content);
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            document.UpdateDescription(request.Description);
        }

        if (!string.IsNullOrWhiteSpace(request.FilePath))
        {
            document.SetFilePath(request.FilePath);
        }

        if (!string.IsNullOrWhiteSpace(request.Tags))
        {
            document.SetTags(request.Tags);
        }

        document.IncrementVersion();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated integration document {Id}, version incremented to {Version}", 
            id, document.Version);

        var documentDto = _mapper.Map<IntegrationDocumentDto>(document);
        return Ok(documentDto);
    }

    /// <summary>
    /// Deactivates an integration document
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateIntegrationDocument(Guid id)
    {
        var document = await _context.IntegrationDocuments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return NotFound($"Integration document with ID {id} not found");
        }

        document.Deactivate();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated integration document {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Reactivates a deactivated integration document
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateIntegrationDocument(Guid id)
    {
        var document = await _context.IntegrationDocuments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return NotFound($"Integration document with ID {id} not found");
        }

        document.Activate();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activated integration document {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Gets all documents for a specific integration system
    /// </summary>
    [HttpGet("by-system/{systemId:guid}")]
    public async Task<ActionResult<List<IntegrationDocumentDto>>> GetDocumentsBySystem(Guid systemId)
    {
        var systemExists = await _context.IntegrationSystems
            .AnyAsync(s => s.Id == systemId);
        if (!systemExists)
        {
            return NotFound($"Integration system with ID {systemId} not found");
        }

        var documents = await _context.IntegrationDocuments
            .Include(d => d.IntegrationSystem)
            .Include(d => d.SystemRelationship)
            .Where(d => d.IntegrationSystemId == systemId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var documentDtos = _mapper.Map<List<IntegrationDocumentDto>>(documents);
        return Ok(documentDtos);
    }

    /// <summary>
    /// Gets document version history
    /// </summary>
    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<List<IntegrationDocumentHistoryDto>>> GetDocumentHistory(Guid id)
    {
        var documentExists = await _context.IntegrationDocuments
            .AnyAsync(d => d.Id == id);
        if (!documentExists)
        {
            return NotFound($"Integration document with ID {id} not found");
        }

        var history = await _context.IntegrationDocumentHistories
            .Where(h => h.IntegrationDocumentId == id)
            .OrderByDescending(h => h.ArchivedAt)
            .ToListAsync();

        var historyDtos = _mapper.Map<List<IntegrationDocumentHistoryDto>>(history);
        return Ok(historyDtos);
    }

    /// <summary>
    /// Gets document statistics by type
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<DocumentStatisticsDto>> GetDocumentStatistics()
    {
        var totalDocuments = await _context.IntegrationDocuments.CountAsync();
        var activeDocuments = await _context.IntegrationDocuments.CountAsync(d => d.IsActive);

        var documentsByType = await _context.IntegrationDocuments
            .Where(d => d.IsActive)
            .GroupBy(d => d.DocumentType)
            .Select(g => new DocumentTypeCountDto
            {
                DocumentType = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var systemsWithDocuments = await _context.IntegrationDocuments
            .Where(d => d.IsActive)
            .Select(d => d.IntegrationSystemId)
            .Distinct()
            .CountAsync();

        var recentlyUpdated = await _context.IntegrationDocuments
            .Where(d => d.IsActive && d.UpdatedAt > DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        var statistics = new DocumentStatisticsDto
        {
            TotalDocuments = totalDocuments,
            ActiveDocuments = activeDocuments,
            InactiveDocuments = totalDocuments - activeDocuments,
            DocumentsByType = documentsByType,
            SystemsWithDocuments = systemsWithDocuments,
            RecentlyUpdatedDocuments = recentlyUpdated
        };

        return Ok(statistics);
    }

    /// <summary>
    /// Searches documents by content
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<List<IntegrationDocumentDto>>> SearchDocuments(
        DocumentSearchRequest request)
    {
        var query = _context.IntegrationDocuments
            .Include(d => d.IntegrationSystem)
            .Include(d => d.SystemRelationship)
            .Where(d => d.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(d => 
                EF.Functions.Like(d.Title, $"%{request.SearchTerm}%") ||
                EF.Functions.Like(d.Content, $"%{request.SearchTerm}%") ||
                EF.Functions.Like(d.Description ?? "", $"%{request.SearchTerm}%") ||
                EF.Functions.Like(d.Tags ?? "", $"%{request.SearchTerm}%"));
        }

        if (request.DocumentTypes != null && request.DocumentTypes.Any())
        {
            query = query.Where(d => request.DocumentTypes.Contains(d.DocumentType));
        }

        if (request.SystemIds != null && request.SystemIds.Any())
        {
            query = query.Where(d => request.SystemIds.Contains(d.IntegrationSystemId));
        }

        var documents = await query
            .OrderByDescending(d => d.UpdatedAt)
            .Take(request.MaxResults)
            .ToListAsync();

        var documentDtos = _mapper.Map<List<IntegrationDocumentDto>>(documents);

        _logger.LogInformation("Document search for '{SearchTerm}' returned {Count} results", 
            request.SearchTerm, documents.Count);

        return Ok(documentDtos);
    }
}
