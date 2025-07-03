using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;
using AutoMapper;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Controller for managing integration mappings between external systems and internal properties
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class IntegrationMappingController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<IntegrationMappingController> _logger;

    public IntegrationMappingController(
        RbacDbContext context, 
        IMapper mapper, 
        ILogger<IntegrationMappingController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all integration mappings with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponseDto<IntegrationMappingDto>>> GetIntegrationMappings(
        [FromQuery] IntegrationMappingSearchRequest request)
    {
        var query = _context.IntegrationMappings
            .Include(m => m.IntegrationSystem)
            .Include(m => m.PropertyDefinition)
            .AsQueryable();

        // Apply filters
        if (request.IntegrationSystemId.HasValue)
        {
            query = query.Where(m => m.IntegrationSystemId == request.IntegrationSystemId.Value);
        }

        if (request.PropertyDefinitionId.HasValue)
        {
            query = query.Where(m => m.PropertyDefinitionId == request.PropertyDefinitionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ExternalFieldName))
        {
            query = query.Where(m => EF.Functions.Like(m.ExternalFieldName, $"%{request.ExternalFieldName}%"));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(m => m.IsActive == request.IsActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var mappings = await query
            .OrderBy(m => m.ExternalFieldName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var mappingDtos = _mapper.Map<List<IntegrationMappingDto>>(mappings);

        var response = new PaginatedResponseDto<IntegrationMappingDto>
        {
            Items = mappingDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        _logger.LogInformation("Retrieved {Count} integration mappings for page {Page}", mappings.Count, request.Page);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific integration mapping by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IntegrationMappingDto>> GetIntegrationMapping(Guid id)
    {
        var mapping = await _context.IntegrationMappings
            .Include(m => m.IntegrationSystem)
            .Include(m => m.PropertyDefinition)
            .Include(m => m.MappingHistories)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mapping == null)
        {
            _logger.LogWarning("Integration mapping with ID {Id} not found", id);
            return NotFound($"Integration mapping with ID {id} not found");
        }

        var mappingDto = _mapper.Map<IntegrationMappingDto>(mapping);
        return Ok(mappingDto);
    }

    /// <summary>
    /// Creates a new integration mapping
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<IntegrationMappingDto>> CreateIntegrationMapping(
        CreateIntegrationMappingRequest request)
    {
        // Validate integration system exists
        var integrationSystemExists = await _context.IntegrationSystems
            .AnyAsync(s => s.Id == request.IntegrationSystemId);
        if (!integrationSystemExists)
        {
            return BadRequest($"Integration system with ID {request.IntegrationSystemId} not found");
        }

        // Validate property definition exists
        var propertyDefinitionExists = await _context.PropertyDefinitions
            .AnyAsync(p => p.Id == request.PropertyDefinitionId);
        if (!propertyDefinitionExists)
        {
            return BadRequest($"Property definition with ID {request.PropertyDefinitionId} not found");
        }

        // Check for duplicate mapping
        var existingMapping = await _context.IntegrationMappings
            .FirstOrDefaultAsync(m => 
                m.IntegrationSystemId == request.IntegrationSystemId &&
                m.ExternalFieldName == request.ExternalFieldName &&
                m.IsActive);

        if (existingMapping != null)
        {
            return BadRequest($"Active mapping already exists for field '{request.ExternalFieldName}' in this system");
        }

        var mapping = IntegrationMapping.Create(
            request.IntegrationSystemId,
            request.PropertyDefinitionId,
            request.ExternalFieldName,
            request.InternalPropertyName,
            request.TransformationRules);

        if (!string.IsNullOrWhiteSpace(request.DefaultValue))
        {
            mapping.SetDefaultValue(request.DefaultValue);
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            mapping.UpdateDescription(request.Description);
        }

        _context.IntegrationMappings.Add(mapping);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created integration mapping {Id} for field {ExternalField}", 
            mapping.Id, request.ExternalFieldName);

        var mappingDto = _mapper.Map<IntegrationMappingDto>(mapping);
        return CreatedAtAction(nameof(GetIntegrationMapping), new { id = mapping.Id }, mappingDto);
    }

    /// <summary>
    /// Updates an existing integration mapping
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IntegrationMappingDto>> UpdateIntegrationMapping(
        Guid id, 
        UpdateIntegrationMappingRequest request)
    {
        var mapping = await _context.IntegrationMappings
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mapping == null)
        {
            return NotFound($"Integration mapping with ID {id} not found");
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.InternalPropertyName))
        {
            mapping.UpdateInternalPropertyName(request.InternalPropertyName);
        }

        if (!string.IsNullOrWhiteSpace(request.TransformationRules))
        {
            mapping.UpdateTransformationRules(request.TransformationRules);
        }

        if (!string.IsNullOrWhiteSpace(request.DefaultValue))
        {
            mapping.SetDefaultValue(request.DefaultValue);
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            mapping.UpdateDescription(request.Description);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated integration mapping {Id}", id);

        var mappingDto = _mapper.Map<IntegrationMappingDto>(mapping);
        return Ok(mappingDto);
    }

    /// <summary>
    /// Deactivates an integration mapping
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateIntegrationMapping(Guid id)
    {
        var mapping = await _context.IntegrationMappings
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mapping == null)
        {
            return NotFound($"Integration mapping with ID {id} not found");
        }

        mapping.Deactivate();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated integration mapping {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Reactivates a deactivated integration mapping
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateIntegrationMapping(Guid id)
    {
        var mapping = await _context.IntegrationMappings
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mapping == null)
        {
            return NotFound($"Integration mapping with ID {id} not found");
        }

        // Check for conflicts with other active mappings
        var conflictingMapping = await _context.IntegrationMappings
            .FirstOrDefaultAsync(m => 
                m.Id != id &&
                m.IntegrationSystemId == mapping.IntegrationSystemId &&
                m.ExternalFieldName == mapping.ExternalFieldName &&
                m.IsActive);

        if (conflictingMapping != null)
        {
            return BadRequest($"Another active mapping already exists for field '{mapping.ExternalFieldName}' in this system");
        }

        mapping.Activate();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activated integration mapping {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Gets integration mapping statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<IntegrationMappingStatisticsDto>> GetIntegrationMappingStatistics()
    {
        var totalMappings = await _context.IntegrationMappings.CountAsync();
        var activeMappings = await _context.IntegrationMappings.CountAsync(m => m.IsActive);
        var inactiveMappings = totalMappings - activeMappings;

        var systemsWithMappings = await _context.IntegrationMappings
            .Select(m => m.IntegrationSystemId)
            .Distinct()
            .CountAsync();

        var propertiesMapped = await _context.IntegrationMappings
            .Where(m => m.IsActive)
            .Select(m => m.PropertyDefinitionId)
            .Distinct()
            .CountAsync();

        var mappingsBySystem = await _context.IntegrationMappings
            .Include(m => m.IntegrationSystem)
            .Where(m => m.IsActive)
            .GroupBy(m => new { m.IntegrationSystemId, m.IntegrationSystem!.Name })
            .Select(g => new SystemMappingCountDto
            {
                SystemId = g.Key.IntegrationSystemId,
                SystemName = g.Key.Name,
                MappingCount = g.Count()
            })
            .OrderByDescending(s => s.MappingCount)
            .ToListAsync();

        var statistics = new IntegrationMappingStatisticsDto
        {
            TotalMappings = totalMappings,
            ActiveMappings = activeMappings,
            InactiveMappings = inactiveMappings,
            SystemsWithMappings = systemsWithMappings,
            PropertiesMapped = propertiesMapped,
            MappingsBySystem = mappingsBySystem
        };

        return Ok(statistics);
    }

    /// <summary>
    /// Gets integration mappings for a specific system
    /// </summary>
    [HttpGet("by-system/{systemId:guid}")]
    public async Task<ActionResult<List<IntegrationMappingDto>>> GetMappingsBySystem(Guid systemId)
    {
        var systemExists = await _context.IntegrationSystems
            .AnyAsync(s => s.Id == systemId);
        if (!systemExists)
        {
            return NotFound($"Integration system with ID {systemId} not found");
        }

        var mappings = await _context.IntegrationMappings
            .Include(m => m.IntegrationSystem)
            .Include(m => m.PropertyDefinition)
            .Where(m => m.IntegrationSystemId == systemId && m.IsActive)
            .OrderBy(m => m.ExternalFieldName)
            .ToListAsync();

        var mappingDtos = _mapper.Map<List<IntegrationMappingDto>>(mappings);
        return Ok(mappingDtos);
    }
}
