using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntityInstancesController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<EntityInstancesController> _logger;

    public EntityInstancesController(RbacDbContext context, IMapper mapper, ILogger<EntityInstancesController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all entity instances with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<EntityInstanceDto>>> GetEntityInstances(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? entityDefinitionId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] SyncStatus? syncStatus = null)
    {
        try
        {
            var query = _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                    .ThenInclude(pv => pv.PropertyDefinition)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.DisplayName.Contains(search) || 
                                        x.ExternalId.Contains(search));
            }

            if (entityDefinitionId.HasValue)
            {
                query = query.Where(x => x.EntityDefinitionId == entityDefinitionId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            if (syncStatus.HasValue)
            {
                query = query.Where(x => x.SyncStatus == syncStatus.Value);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(x => x.DisplayName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<EntityInstanceDto>>(items);

            // Map property values for each entity instance
            foreach (var dto in dtos)
            {
                var entityInstance = items.First(x => x.Id == dto.Id);
                dto.PropertyValues = _mapper.Map<List<PropertyValueDto>>(entityInstance.PropertyValues);
            }

            var result = new PagedResult<EntityInstanceDto>
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
            _logger.LogError(ex, "Error getting entity instances");
            return StatusCode(500, "An error occurred while retrieving entity instances");
        }
    }

    /// <summary>
    /// Get a specific entity instance by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EntityInstanceDto>> GetEntityInstance(Guid id)
    {
        try
        {
            var entityInstance = await _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                    .ThenInclude(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entityInstance == null)
            {
                return NotFound($"Entity instance with ID {id} not found");
            }

            var dto = _mapper.Map<EntityInstanceDto>(entityInstance);
            dto.PropertyValues = _mapper.Map<List<PropertyValueDto>>(entityInstance.PropertyValues);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity instance {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the entity instance");
        }
    }

    /// <summary>
    /// Create a new entity instance
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EntityInstanceDto>> CreateEntityInstance(CreateEntityInstanceDto createDto)
    {
        try
        {
            // Check if entity definition exists
            var entityDefinition = await _context.EntityDefinitions
                .Include(x => x.PropertyDefinitions)
                .FirstOrDefaultAsync(x => x.Id == createDto.EntityDefinitionId);

            if (entityDefinition == null)
            {
                return BadRequest($"Entity definition with ID {createDto.EntityDefinitionId} not found");
            }

            // Check if external ID already exists within the entity definition
            var existingInstance = await _context.EntityInstances
                .FirstOrDefaultAsync(x => x.EntityDefinitionId == createDto.EntityDefinitionId && 
                                         x.ExternalId == createDto.ExternalId);

            if (existingInstance != null)
            {
                return Conflict($"Entity instance with external ID '{createDto.ExternalId}' already exists in this entity definition");
            }

            var entityInstance = _mapper.Map<EntityInstance>(createDto);
            entityInstance.SyncStatus = SyncStatus.Success;
            entityInstance.LastSyncedAt = DateTime.UtcNow;
            
            _context.EntityInstances.Add(entityInstance);
            await _context.SaveChangesAsync();

            // Create property values
            var propertyValues = new List<PropertyValue>();
            foreach (var propValueDto in createDto.PropertyValues)
            {
                // Validate property definition exists
                var propertyDefinition = entityDefinition.PropertyDefinitions
                    .FirstOrDefault(pd => pd.Id == propValueDto.PropertyDefinitionId);

                if (propertyDefinition == null)
                {
                    return BadRequest($"Property definition with ID {propValueDto.PropertyDefinitionId} not found in this entity definition");
                }

                var propertyValue = _mapper.Map<PropertyValue>(propValueDto);
                propertyValue.EntityInstanceId = entityInstance.Id;
                propertyValues.Add(propertyValue);
            }

            if (propertyValues.Any())
            {
                _context.PropertyValues.AddRange(propertyValues);
                await _context.SaveChangesAsync();
            }

            // Reload with navigation properties
            entityInstance = await _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                    .ThenInclude(pv => pv.PropertyDefinition)
                .FirstAsync(x => x.Id == entityInstance.Id);

            var dto = _mapper.Map<EntityInstanceDto>(entityInstance);
            dto.PropertyValues = _mapper.Map<List<PropertyValueDto>>(entityInstance.PropertyValues);
            
            return CreatedAtAction(nameof(GetEntityInstance), 
                new { id = entityInstance.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity instance");
            return StatusCode(500, "An error occurred while creating the entity instance");
        }
    }

    /// <summary>
    /// Update an existing entity instance
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EntityInstanceDto>> UpdateEntityInstance(Guid id, UpdateEntityInstanceDto updateDto)
    {
        try
        {
            var entityInstance = await _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entityInstance == null)
            {
                return NotFound($"Entity instance with ID {id} not found");
            }

            // Check if entity definition exists
            var entityDefinition = await _context.EntityDefinitions
                .Include(x => x.PropertyDefinitions)
                .FirstOrDefaultAsync(x => x.Id == updateDto.EntityDefinitionId);

            if (entityDefinition == null)
            {
                return BadRequest($"Entity definition with ID {updateDto.EntityDefinitionId} not found");
            }

            // Check if external ID already exists (excluding current record)
            var existingInstance = await _context.EntityInstances
                .FirstOrDefaultAsync(x => x.EntityDefinitionId == updateDto.EntityDefinitionId && 
                                         x.ExternalId == updateDto.ExternalId && 
                                         x.Id != id);

            if (existingInstance != null)
            {
                return Conflict($"Entity instance with external ID '{updateDto.ExternalId}' already exists in this entity definition");
            }

            // Update entity instance
            _mapper.Map(updateDto, entityInstance);
            entityInstance.LastModifiedReason = updateDto.LastModifiedReason;

            // Update property values
            var existingPropertyValues = entityInstance.PropertyValues.ToList();
            
            // Remove property values not in the update
            var propertyValuesToRemove = existingPropertyValues
                .Where(pv => !updateDto.PropertyValues.Any(upv => upv.Id == pv.Id))
                .ToList();

            _context.PropertyValues.RemoveRange(propertyValuesToRemove);

            // Update existing and add new property values
            foreach (var propValueDto in updateDto.PropertyValues)
            {
                // Validate property definition exists
                var propertyDefinition = entityDefinition.PropertyDefinitions
                    .FirstOrDefault(pd => pd.Id == propValueDto.PropertyDefinitionId);

                if (propertyDefinition == null)
                {
                    return BadRequest($"Property definition with ID {propValueDto.PropertyDefinitionId} not found in this entity definition");
                }

                if (propValueDto.Id.HasValue)
                {
                    // Update existing property value
                    var existingPropertyValue = existingPropertyValues
                        .FirstOrDefault(pv => pv.Id == propValueDto.Id.Value);

                    if (existingPropertyValue != null)
                    {
                        _mapper.Map(propValueDto, existingPropertyValue);
                    }
                }
                else
                {
                    // Add new property value
                    var newPropertyValue = _mapper.Map<PropertyValue>(propValueDto);
                    newPropertyValue.EntityInstanceId = entityInstance.Id;
                    _context.PropertyValues.Add(newPropertyValue);
                }
            }

            await _context.SaveChangesAsync();

            // Reload with navigation properties
            entityInstance = await _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                    .ThenInclude(pv => pv.PropertyDefinition)
                .FirstAsync(x => x.Id == entityInstance.Id);

            var dto = _mapper.Map<EntityInstanceDto>(entityInstance);
            dto.PropertyValues = _mapper.Map<List<PropertyValueDto>>(entityInstance.PropertyValues);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity instance {Id}", id);
            return StatusCode(500, "An error occurred while updating the entity instance");
        }
    }

    /// <summary>
    /// Delete an entity instance (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntityInstance(Guid id)
    {
        try
        {
            var entityInstance = await _context.EntityInstances
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entityInstance == null)
            {
                return NotFound($"Entity instance with ID {id} not found");
            }

            // Check if there are dependent records (access assignments)
            var hasAccessAssignments = await _context.AccessAssignments
                .AnyAsync(x => x.UserId == id || x.RoleId == id);

            if (hasAccessAssignments)
            {
                return BadRequest("Cannot delete entity instance with existing access assignments");
            }

            // Soft delete
            entityInstance.IsDeleted = true;
            entityInstance.DeletedAt = DateTime.UtcNow;
            entityInstance.DeletedBy = "system"; // TODO: Get from current user context

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity instance {Id}", id);
            return StatusCode(500, "An error occurred while deleting the entity instance");
        }
    }
}
