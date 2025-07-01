using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PropertyValuesController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PropertyValuesController> _logger;

    public PropertyValuesController(RbacDbContext context, IMapper mapper, ILogger<PropertyValuesController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all property values with optional filtering and pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="entityInstanceId">Filter by entity instance ID</param>
    /// <param name="propertyDefinitionId">Filter by property definition ID</param>
    /// <param name="isDefault">Filter by default status</param>
    /// <param name="search">Search in value and display value</param>
    /// <returns>Paginated list of property values</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PropertyValueDto>>> GetPropertyValues(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? entityInstanceId = null,
        [FromQuery] Guid? propertyDefinitionId = null,
        [FromQuery] bool? isDefault = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .AsQueryable();

            // Apply filters
            if (entityInstanceId.HasValue)
                query = query.Where(pv => pv.EntityInstanceId == entityInstanceId.Value);

            if (propertyDefinitionId.HasValue)
                query = query.Where(pv => pv.PropertyDefinitionId == propertyDefinitionId.Value);

            if (isDefault.HasValue)
                query = query.Where(pv => pv.IsDefault == isDefault.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(pv => 
                    pv.Value.ToLower().Contains(searchLower) ||
                    (pv.DisplayValue != null && pv.DisplayValue.ToLower().Contains(searchLower)) ||
                    pv.EntityInstance.DisplayName.ToLower().Contains(searchLower) ||
                    pv.PropertyDefinition.Name.ToLower().Contains(searchLower));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(pv => pv.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<PropertyValueDto>>(items);

            var result = new PagedResult<PropertyValueDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property values");
            return StatusCode(500, "An error occurred while retrieving property values");
        }
    }

    /// <summary>
    /// Get property value by ID
    /// </summary>
    /// <param name="id">Property value ID</param>
    /// <returns>Property value details</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyValueDto>> GetPropertyValue(Guid id)
    {
        try
        {
            var propertyValue = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(pv => pv.Id == id);

            if (propertyValue == null)
                return NotFound($"Property value with ID {id} not found");

            var dto = _mapper.Map<PropertyValueDto>(propertyValue);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property value {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the property value");
        }
    }

    /// <summary>
    /// Create a new property value
    /// </summary>
    /// <param name="dto">Property value creation data</param>
    /// <returns>Created property value</returns>
    [HttpPost]
    public async Task<ActionResult<PropertyValueDto>> CreatePropertyValue(CreatePropertyValueDto dto)
    {
        try
        {
            // Validate referenced entities exist
            var entityInstanceExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.EntityInstanceId);
            if (!entityInstanceExists)
                return BadRequest($"Entity instance with ID {dto.EntityInstanceId} not found");

            var propertyDefinitionExists = await _context.PropertyDefinitions.AnyAsync(pd => pd.Id == dto.PropertyDefinitionId);
            if (!propertyDefinitionExists)
                return BadRequest($"Property definition with ID {dto.PropertyDefinitionId} not found");

            // Check if property value already exists for this entity instance and property definition
            var existingValue = await _context.PropertyValues
                .FirstOrDefaultAsync(pv => 
                    pv.EntityInstanceId == dto.EntityInstanceId && 
                    pv.PropertyDefinitionId == dto.PropertyDefinitionId &&
                    pv.EffectiveTo == null); // Currently effective

            if (existingValue != null)
                return Conflict("A property value already exists for this entity instance and property definition combination");

            var propertyValue = _mapper.Map<PropertyValue>(dto);
            propertyValue.Id = Guid.NewGuid();
            propertyValue.CreatedAt = DateTime.UtcNow;
            propertyValue.CreatedBy = "System"; // In real app, get from user context

            _context.PropertyValues.Add(propertyValue);
            await _context.SaveChangesAsync();

            // Fetch the created property value with navigation properties
            var createdPropertyValue = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(pv => pv.Id == propertyValue.Id);

            var resultDto = _mapper.Map<PropertyValueDto>(createdPropertyValue);
            return CreatedAtAction(nameof(GetPropertyValue), new { id = propertyValue.Id }, resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property value");
            return StatusCode(500, "An error occurred while creating the property value");
        }
    }

    /// <summary>
    /// Update an existing property value
    /// </summary>
    /// <param name="id">Property value ID</param>
    /// <param name="dto">Property value update data</param>
    /// <returns>Updated property value</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PropertyValueDto>> UpdatePropertyValue(Guid id, UpdatePropertyValueDto dto)
    {
        try
        {
            var existingPropertyValue = await _context.PropertyValues.FindAsync(id);
            if (existingPropertyValue == null)
                return NotFound($"Property value with ID {id} not found");

            // Validate referenced entities exist
            var entityInstanceExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.EntityInstanceId);
            if (!entityInstanceExists)
                return BadRequest($"Entity instance with ID {dto.EntityInstanceId} not found");

            var propertyDefinitionExists = await _context.PropertyDefinitions.AnyAsync(pd => pd.Id == dto.PropertyDefinitionId);
            if (!propertyDefinitionExists)
                return BadRequest($"Property definition with ID {dto.PropertyDefinitionId} not found");

            // Check for conflicting property value (if changing key fields)
            if (existingPropertyValue.EntityInstanceId != dto.EntityInstanceId || 
                existingPropertyValue.PropertyDefinitionId != dto.PropertyDefinitionId)
            {
                var conflictingValue = await _context.PropertyValues
                    .FirstOrDefaultAsync(pv => 
                        pv.Id != id &&
                        pv.EntityInstanceId == dto.EntityInstanceId && 
                        pv.PropertyDefinitionId == dto.PropertyDefinitionId &&
                        pv.EffectiveTo == null);

                if (conflictingValue != null)
                    return Conflict("A property value already exists for this entity instance and property definition combination");
            }

            _mapper.Map(dto, existingPropertyValue);
            existingPropertyValue.UpdatedAt = DateTime.UtcNow;
            existingPropertyValue.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            // Fetch updated property value with navigation properties
            var updatedPropertyValue = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(pv => pv.Id == id);

            var resultDto = _mapper.Map<PropertyValueDto>(updatedPropertyValue);
            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property value {Id}", id);
            return StatusCode(500, "An error occurred while updating the property value");
        }
    }

    /// <summary>
    /// Delete a property value
    /// </summary>
    /// <param name="id">Property value ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeletePropertyValue(Guid id)
    {
        try
        {
            var propertyValue = await _context.PropertyValues.FindAsync(id);
            if (propertyValue == null)
                return NotFound($"Property value with ID {id} not found");

            // Soft delete
            propertyValue.IsDeleted = true;
            propertyValue.DeletedAt = DateTime.UtcNow;
            propertyValue.DeletedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting property value {Id}", id);
            return StatusCode(500, "An error occurred while deleting the property value");
        }
    }

    /// <summary>
    /// Get property values for a specific entity instance
    /// </summary>
    /// <param name="entityInstanceId">Entity instance ID</param>
    /// <param name="includeHistory">Include historical (expired) values</param>
    /// <returns>List of entity instance's property values</returns>
    [HttpGet("entity-instance/{entityInstanceId:guid}")]
    public async Task<ActionResult<List<PropertyValueDto>>> GetEntityInstancePropertyValues(
        Guid entityInstanceId, 
        [FromQuery] bool includeHistory = false)
    {
        try
        {
            var query = _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .Where(pv => pv.EntityInstanceId == entityInstanceId);

            if (!includeHistory)
                query = query.Where(pv => pv.EffectiveTo == null || pv.EffectiveTo > DateTime.UtcNow);

            var propertyValues = await query
                .OrderBy(pv => pv.PropertyDefinition.Name)
                .ThenByDescending(pv => pv.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<PropertyValueDto>>(propertyValues);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property values for entity instance {EntityInstanceId}", entityInstanceId);
            return StatusCode(500, "An error occurred while retrieving property values");
        }
    }

    /// <summary>
    /// Get property values for a specific property definition
    /// </summary>
    /// <param name="propertyDefinitionId">Property definition ID</param>
    /// <param name="includeHistory">Include historical (expired) values</param>
    /// <returns>List of property definition's values</returns>
    [HttpGet("property-definition/{propertyDefinitionId:guid}")]
    public async Task<ActionResult<List<PropertyValueDto>>> GetPropertyDefinitionValues(
        Guid propertyDefinitionId, 
        [FromQuery] bool includeHistory = false)
    {
        try
        {
            var query = _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .Where(pv => pv.PropertyDefinitionId == propertyDefinitionId);

            if (!includeHistory)
                query = query.Where(pv => pv.EffectiveTo == null || pv.EffectiveTo > DateTime.UtcNow);

            var propertyValues = await query
                .OrderBy(pv => pv.EntityInstance.DisplayName)
                .ThenByDescending(pv => pv.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<PropertyValueDto>>(propertyValues);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property values for property definition {PropertyDefinitionId}", propertyDefinitionId);
            return StatusCode(500, "An error occurred while retrieving property values");
        }
    }

    /// <summary>
    /// Get property value history for an entity instance grouped by property definition
    /// </summary>
    /// <param name="entityInstanceId">Entity instance ID</param>
    /// <returns>Property value history grouped by property definition</returns>
    [HttpGet("entity-instance/{entityInstanceId:guid}/history")]
    public async Task<ActionResult<List<PropertyValueHistoryDto>>> GetEntityInstancePropertyValueHistory(Guid entityInstanceId)
    {
        try
        {
            var propertyValues = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .Where(pv => pv.EntityInstanceId == entityInstanceId)
                .OrderBy(pv => pv.PropertyDefinition.Name)
                .ThenByDescending(pv => pv.CreatedAt)
                .ToListAsync();

            var groupedHistory = propertyValues
                .GroupBy(pv => new { pv.PropertyDefinitionId, pv.PropertyDefinition.Name })
                .Select(g => new PropertyValueHistoryDto
                {
                    PropertyDefinitionId = g.Key.PropertyDefinitionId,
                    PropertyDefinitionName = g.Key.Name,
                    ValueHistory = _mapper.Map<List<PropertyValueDto>>(g.ToList())
                })
                .ToList();

            return Ok(groupedHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property value history for entity instance {EntityInstanceId}", entityInstanceId);
            return StatusCode(500, "An error occurred while retrieving property value history");
        }
    }

    /// <summary>
    /// Set property value effective end date (expire it)
    /// </summary>
    /// <param name="id">Property value ID</param>
    /// <param name="effectiveTo">Effective end date</param>
    /// <returns>Updated property value</returns>
    [HttpPatch("{id:guid}/expire")]
    public async Task<ActionResult<PropertyValueDto>> ExpirePropertyValue(Guid id, [FromBody] DateTime effectiveTo)
    {
        try
        {
            var propertyValue = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(pv => pv.Id == id);

            if (propertyValue == null)
                return NotFound($"Property value with ID {id} not found");

            propertyValue.EffectiveTo = effectiveTo;
            propertyValue.UpdatedAt = DateTime.UtcNow;
            propertyValue.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<PropertyValueDto>(propertyValue);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring property value {Id}", id);
            return StatusCode(500, "An error occurred while expiring the property value");
        }
    }
}
