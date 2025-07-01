using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntityDefinitionsController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<EntityDefinitionsController> _logger;

    public EntityDefinitionsController(RbacDbContext context, IMapper mapper, ILogger<EntityDefinitionsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all entity definitions with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<EntityDefinitionDto>>> GetEntityDefinitions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? integrationSystemId = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.EntityDefinitions
                .Include(x => x.IntegrationSystem)
                .Include(x => x.PropertyDefinitions)
                .Include(x => x.EntityInstances)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Name.Contains(search) || 
                                        x.DisplayName.Contains(search) || 
                                        x.Description.Contains(search));
            }

            if (integrationSystemId.HasValue)
            {
                query = query.Where(x => x.IntegrationSystemId == integrationSystemId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<EntityDefinitionDto>>(items);

            var result = new PagedResult<EntityDefinitionDto>
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
            _logger.LogError(ex, "Error getting entity definitions");
            return StatusCode(500, "An error occurred while retrieving entity definitions");
        }
    }

    /// <summary>
    /// Get a specific entity definition by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EntityDefinitionDto>> GetEntityDefinition(Guid id)
    {
        try
        {
            var entityDefinition = await _context.EntityDefinitions
                .Include(x => x.IntegrationSystem)
                .Include(x => x.PropertyDefinitions)
                .Include(x => x.EntityInstances)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entityDefinition == null)
            {
                return NotFound($"Entity definition with ID {id} not found");
            }

            var dto = _mapper.Map<EntityDefinitionDto>(entityDefinition);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity definition {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the entity definition");
        }
    }

    /// <summary>
    /// Create a new entity definition
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EntityDefinitionDto>> CreateEntityDefinition(CreateEntityDefinitionDto createDto)
    {
        try
        {
            // Check if integration system exists
            var integrationSystemExists = await _context.IntegrationSystems
                .AnyAsync(x => x.Id == createDto.IntegrationSystemId);

            if (!integrationSystemExists)
            {
                return BadRequest($"Integration system with ID {createDto.IntegrationSystemId} not found");
            }

            // Check if name already exists within the integration system
            var existingDefinition = await _context.EntityDefinitions
                .FirstOrDefaultAsync(x => x.IntegrationSystemId == createDto.IntegrationSystemId && 
                                         x.Name == createDto.Name);

            if (existingDefinition != null)
            {
                return Conflict($"Entity definition with name '{createDto.Name}' already exists in this integration system");
            }

            var entityDefinition = _mapper.Map<EntityDefinition>(createDto);
            
            _context.EntityDefinitions.Add(entityDefinition);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            entityDefinition = await _context.EntityDefinitions
                .Include(x => x.IntegrationSystem)
                .FirstAsync(x => x.Id == entityDefinition.Id);

            var dto = _mapper.Map<EntityDefinitionDto>(entityDefinition);
            
            return CreatedAtAction(nameof(GetEntityDefinition), 
                new { id = entityDefinition.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity definition");
            return StatusCode(500, "An error occurred while creating the entity definition");
        }
    }

    /// <summary>
    /// Update an existing entity definition
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EntityDefinitionDto>> UpdateEntityDefinition(Guid id, UpdateEntityDefinitionDto updateDto)
    {
        try
        {
            var entityDefinition = await _context.EntityDefinitions
                .Include(x => x.IntegrationSystem)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entityDefinition == null)
            {
                return NotFound($"Entity definition with ID {id} not found");
            }

            // Check if integration system exists
            var integrationSystemExists = await _context.IntegrationSystems
                .AnyAsync(x => x.Id == updateDto.IntegrationSystemId);

            if (!integrationSystemExists)
            {
                return BadRequest($"Integration system with ID {updateDto.IntegrationSystemId} not found");
            }

            // Check if name already exists within the integration system (excluding current record)
            var existingDefinition = await _context.EntityDefinitions
                .FirstOrDefaultAsync(x => x.IntegrationSystemId == updateDto.IntegrationSystemId && 
                                         x.Name == updateDto.Name && 
                                         x.Id != id);

            if (existingDefinition != null)
            {
                return Conflict($"Entity definition with name '{updateDto.Name}' already exists in this integration system");
            }

            _mapper.Map(updateDto, entityDefinition);
            entityDefinition.LastModifiedReason = updateDto.LastModifiedReason;

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<EntityDefinitionDto>(entityDefinition);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity definition {Id}", id);
            return StatusCode(500, "An error occurred while updating the entity definition");
        }
    }

    /// <summary>
    /// Delete an entity definition (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntityDefinition(Guid id)
    {
        try
        {
            var entityDefinition = await _context.EntityDefinitions
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entityDefinition == null)
            {
                return NotFound($"Entity definition with ID {id} not found");
            }

            // Check if there are dependent records
            var hasEntityInstances = await _context.EntityInstances
                .AnyAsync(x => x.EntityDefinitionId == id);

            if (hasEntityInstances)
            {
                return BadRequest("Cannot delete entity definition with existing entity instances");
            }

            // Soft delete
            entityDefinition.IsDeleted = true;
            entityDefinition.DeletedAt = DateTime.UtcNow;
            entityDefinition.DeletedBy = "system"; // TODO: Get from current user context

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity definition {Id}", id);
            return StatusCode(500, "An error occurred while deleting the entity definition");
        }
    }

    /// <summary>
    /// Get property definitions for a specific entity definition
    /// </summary>
    [HttpGet("{id}/property-definitions")]
    public async Task<ActionResult<IEnumerable<PropertyDefinitionDto>>> GetPropertyDefinitions(Guid id)
    {
        try
        {
            var entityDefinitionExists = await _context.EntityDefinitions
                .AnyAsync(x => x.Id == id);

            if (!entityDefinitionExists)
            {
                return NotFound($"Entity definition with ID {id} not found");
            }

            var propertyDefinitions = await _context.PropertyDefinitions
                .Include(x => x.EntityDefinition)
                .Where(x => x.EntityDefinitionId == id)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<PropertyDefinitionDto>>(propertyDefinitions);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property definitions for entity definition {Id}", id);
            return StatusCode(500, "An error occurred while retrieving property definitions");
        }
    }
}
