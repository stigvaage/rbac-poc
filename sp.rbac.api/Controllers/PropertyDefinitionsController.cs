using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropertyDefinitionsController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PropertyDefinitionsController> _logger;

    public PropertyDefinitionsController(RbacDbContext context, IMapper mapper, ILogger<PropertyDefinitionsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all property definitions with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PropertyDefinitionDto>>> GetPropertyDefinitions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? entityDefinitionId = null,
        [FromQuery] DataType? dataType = null)
    {
        try
        {
            var query = _context.PropertyDefinitions
                .Include(x => x.EntityDefinition)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Name.Contains(search) || 
                                        x.DisplayName.Contains(search) || 
                                        x.Description.Contains(search));
            }

            if (entityDefinitionId.HasValue)
            {
                query = query.Where(x => x.EntityDefinitionId == entityDefinitionId.Value);
            }

            if (dataType.HasValue)
            {
                query = query.Where(x => x.DataType == dataType.Value);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<PropertyDefinitionDto>>(items);

            var result = new PagedResult<PropertyDefinitionDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property definitions");
            return StatusCode(500, "An error occurred while retrieving property definitions");
        }
    }

    /// <summary>
    /// Get a specific property definition by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PropertyDefinitionDto>> GetPropertyDefinition(Guid id)
    {
        try
        {
            var propertyDefinition = await _context.PropertyDefinitions
                .Include(x => x.EntityDefinition)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (propertyDefinition == null)
            {
                return NotFound($"Property definition with ID {id} not found");
            }

            var dto = _mapper.Map<PropertyDefinitionDto>(propertyDefinition);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property definition {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the property definition");
        }
    }

    /// <summary>
    /// Create a new property definition
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PropertyDefinitionDto>> CreatePropertyDefinition(CreatePropertyDefinitionDto createDto)
    {
        try
        {
            // Check if entity definition exists
            var entityDefinitionExists = await _context.EntityDefinitions
                .AnyAsync(x => x.Id == createDto.EntityDefinitionId);

            if (!entityDefinitionExists)
            {
                return BadRequest($"Entity definition with ID {createDto.EntityDefinitionId} not found");
            }

            // Check if name already exists within the entity definition
            var existingProperty = await _context.PropertyDefinitions
                .FirstOrDefaultAsync(x => x.EntityDefinitionId == createDto.EntityDefinitionId && 
                                         x.Name == createDto.Name);

            if (existingProperty != null)
            {
                return Conflict($"Property definition with name '{createDto.Name}' already exists in this entity definition");
            }

            var propertyDefinition = _mapper.Map<PropertyDefinition>(createDto);
            
            _context.PropertyDefinitions.Add(propertyDefinition);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            propertyDefinition = await _context.PropertyDefinitions
                .Include(x => x.EntityDefinition)
                .FirstAsync(x => x.Id == propertyDefinition.Id);

            var dto = _mapper.Map<PropertyDefinitionDto>(propertyDefinition);
            
            return CreatedAtAction(nameof(GetPropertyDefinition), 
                new { id = propertyDefinition.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property definition");
            return StatusCode(500, "An error occurred while creating the property definition");
        }
    }

    /// <summary>
    /// Update an existing property definition
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PropertyDefinitionDto>> UpdatePropertyDefinition(Guid id, UpdatePropertyDefinitionDto updateDto)
    {
        try
        {
            var propertyDefinition = await _context.PropertyDefinitions
                .Include(x => x.EntityDefinition)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (propertyDefinition == null)
            {
                return NotFound($"Property definition with ID {id} not found");
            }

            // Check if entity definition exists
            var entityDefinitionExists = await _context.EntityDefinitions
                .AnyAsync(x => x.Id == updateDto.EntityDefinitionId);

            if (!entityDefinitionExists)
            {
                return BadRequest($"Entity definition with ID {updateDto.EntityDefinitionId} not found");
            }

            // Check if name already exists within the entity definition (excluding current record)
            var existingProperty = await _context.PropertyDefinitions
                .FirstOrDefaultAsync(x => x.EntityDefinitionId == updateDto.EntityDefinitionId && 
                                         x.Name == updateDto.Name && 
                                         x.Id != id);

            if (existingProperty != null)
            {
                return Conflict($"Property definition with name '{updateDto.Name}' already exists in this entity definition");
            }

            _mapper.Map(updateDto, propertyDefinition);
            propertyDefinition.LastModifiedReason = updateDto.LastModifiedReason;

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<PropertyDefinitionDto>(propertyDefinition);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property definition {Id}", id);
            return StatusCode(500, "An error occurred while updating the property definition");
        }
    }

    /// <summary>
    /// Delete a property definition (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePropertyDefinition(Guid id)
    {
        try
        {
            var propertyDefinition = await _context.PropertyDefinitions
                .FirstOrDefaultAsync(x => x.Id == id);

            if (propertyDefinition == null)
            {
                return NotFound($"Property definition with ID {id} not found");
            }

            // Check if there are dependent records
            var hasPropertyValues = await _context.PropertyValues
                .AnyAsync(x => x.PropertyDefinitionId == id);

            if (hasPropertyValues)
            {
                return BadRequest("Cannot delete property definition with existing property values");
            }

            // Soft delete
            propertyDefinition.IsDeleted = true;
            propertyDefinition.DeletedAt = DateTime.UtcNow;
            propertyDefinition.DeletedBy = "system"; // TODO: Get from current user context

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting property definition {Id}", id);
            return StatusCode(500, "An error occurred while deleting the property definition");
        }
    }

    /// <summary>
    /// Get available data types
    /// </summary>
    [HttpGet("data-types")]
    public ActionResult<IEnumerable<object>> GetDataTypes()
    {
        try
        {
            var dataTypes = Enum.GetValues<DataType>()
                .Select(dt => new { Value = (int)dt, Name = dt.ToString() })
                .ToList();

            return Ok(dataTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data types");
            return StatusCode(500, "An error occurred while retrieving data types");
        }
    }
}
