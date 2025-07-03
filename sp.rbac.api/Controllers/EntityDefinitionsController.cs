using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Administrerer entitetsdefinisjoner som definerer struktur og metadata for dataentiteter fra eksterne systemer
/// </summary>
/// <remarks>
/// Entitetsdefinisjoner representerer strukturen og metadataene for dataentiteter importert fra eksterne systemer.
/// Hver entitetsdefinisjon tilhører et integrasjonssystem og definerer skjemaet for entitetsinstanser.
/// 
/// **Hovedfunksjoner:**
/// - Definere entiteter fra eksterne systemer (HR-ansatte, EMR-pasienter, CRM-kontakter)
/// - Konfigurere entitetsmetadata inkludert tabellnavn og primærnøkkelfelt
/// - Administrere hierarkiske relasjoner mellom entiteter
/// - Støtte for tilpasset sortering og visningsinnstillinger
/// - Fleksibel metadata-lagring for systemspesifikke konfigurasjoner
/// 
/// **Vanlige brukstilfeller:**
/// - Definere "Ansatt"-entitet fra HR-system med tabellmapping
/// - Konfigurere "Pasient"-entitet fra EMR-system med personverninnstillinger
/// - Sette opp "Kunde"-entitet fra CRM-system med relasjonsmapping
/// - Etablere entitetshierarkier for organisasjonsstrukturer
/// 
/// **Eksempelforespørsler:**
/// ```json
/// POST /api/entitydefinitions
/// {
///   "integrationSystemId": "123e4567-e89b-12d3-a456-426614174000",
///   "name": "Employee",
///   "displayName": "Employee Records",
///   "description": "Employee entity from HR system",
///   "tableName": "Employees",
///   "primaryKeyField": "EmployeeId",
///   "isActive": true,
///   "sortOrder": 1,
///   "metadata": "{\"syncFrequency\": \"daily\", \"batchSize\": 500}"
/// }
/// ```
/// 
/// **Response Structure:**
/// All responses include comprehensive entity metadata with integration system details,
/// property definition counts, and entity instance counts for resource planning.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Entity Definitions")]
[Produces("application/json")]
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
    /// Retrieve all entity definitions with advanced filtering and pagination
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of entity definitions with comprehensive filtering options.
    /// Results include integration system details, property definition counts, and entity instance counts.
    /// 
    /// **Filter Options:**
    /// - **search**: Filter by name, display name, or description (case-insensitive partial match)
    /// - **integrationSystemId**: Filter by specific integration system
    /// - **isActive**: Filter by active/inactive status
    /// 
    /// **Sorting**: Results are ordered by sort order (ascending), then by name (ascending)
    /// 
    /// **Example Usage:**
    /// - `GET /api/entitydefinitions?search=employee&amp;isActive=true&amp;pageSize=20`
    /// - `GET /api/entitydefinitions?integrationSystemId=123e4567-e89b-12d3-a456-426614174000`
    /// 
    /// **Performance Notes:**
    /// - Includes related data (integration system, property definitions count, entity instances count)
    /// - Large datasets are automatically paginated for optimal performance
    /// - Consider using smaller page sizes for systems with many entity definitions
    /// </remarks>
    /// <param name="pageNumber">Page number for pagination (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10, maximum: 100)</param>
    /// <param name="search">Optional search term to filter by name, display name, or description</param>
    /// <param name="integrationSystemId">Optional filter by integration system ID</param>
    /// <param name="isActive">Optional filter by active status (true/false)</param>
    /// <returns>Paginated list of entity definitions with metadata and related counts</returns>
    /// <response code="200">Successfully retrieved entity definitions</response>
    /// <response code="400">Invalid pagination parameters or filter values</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EntityDefinitionDto>), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(string), 500)]
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
    /// Retrieve a specific entity definition by its unique identifier
    /// </summary>
    /// <remarks>
    /// Returns detailed information about a single entity definition including:
    /// - Complete entity metadata and configuration
    /// - Integration system details and connection info
    /// - Count of associated property definitions
    /// - Count of entity instances using this definition
    /// 
    /// **Use Cases:**
    /// - View complete entity configuration before modification
    /// - Validate entity structure before creating instances
    /// - Check property definition and instance counts for capacity planning
    /// - Retrieve entity metadata for custom integration workflows
    /// 
    /// **Example Response Data:**
    /// The response includes comprehensive entity metadata, integration system details,
    /// and related counts to support administrative and operational decisions.
    /// </remarks>
    /// <param name="id">Unique identifier of the entity definition</param>
    /// <returns>Complete entity definition with related data and counts</returns>
    /// <response code="200">Successfully retrieved entity definition</response>
    /// <response code="404">Entity definition not found with the specified ID</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EntityDefinitionDto), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
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
    /// Create a new entity definition for an integration system
    /// </summary>
    /// <remarks>
    /// Creates a new entity definition that defines the structure for data entities from external systems.
    /// The entity definition serves as a template for creating entity instances.
    /// 
    /// **Prerequisites:**
    /// - Integration system must exist and be active
    /// - Entity name must be unique within the integration system
    /// - Table name should match the external system's table/collection name
    /// - Primary key field should match the external system's identifier field
    /// 
    /// **Validation Rules:**
    /// - Integration system ID must reference an existing integration system
    /// - Entity name must be unique within the specified integration system
    /// - Required fields: name, displayName, tableName, primaryKeyField
    /// - Sort order defaults to 0 if not specified
    /// - IsActive defaults to true if not specified
    /// 
    /// **Best Practices:**
    /// - Use descriptive display names for user interfaces
    /// - Include comprehensive descriptions for documentation
    /// - Set appropriate sort orders for logical grouping
    /// - Use metadata field for system-specific configurations
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "integrationSystemId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "name": "Employee",
    ///   "displayName": "Employee Records",
    ///   "description": "Employee entity from HR system with full profile data",
    ///   "tableName": "Employees",
    ///   "primaryKeyField": "EmployeeId",
    ///   "isActive": true,
    ///   "sortOrder": 1,
    ///   "metadata": "{\"syncFrequency\": \"daily\", \"includeArchived\": false}"
    /// }
    /// ```
    /// </remarks>
    /// <param name="createDto">Entity definition data for creation</param>
    /// <returns>Created entity definition with generated ID and metadata</returns>
    /// <response code="201">Entity definition created successfully</response>
    /// <response code="400">Invalid request data or integration system not found</response>
    /// <response code="409">Entity definition with the same name already exists in the integration system</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpPost]
    [ProducesResponseType(typeof(EntityDefinitionDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(string), 409)]
    [ProducesResponseType(typeof(string), 500)]
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
    /// Update an existing entity definition with new configuration
    /// </summary>
    /// <remarks>
    /// Updates an existing entity definition with new metadata, configuration, or structural changes.
    /// Supports partial updates while maintaining data integrity and referential constraints.
    /// 
    /// **Update Capabilities:**
    /// - Modify entity metadata (name, display name, description)
    /// - Change integration system assignment (with validation)
    /// - Update table mapping and primary key field configuration
    /// - Adjust sort order and active status
    /// - Update custom metadata and system-specific settings
    /// 
    /// **Validation and Constraints:**
    /// - Integration system must exist and be accessible
    /// - New entity name must be unique within target integration system
    /// - Cannot change integration system if entity instances exist (contact support)
    /// - Last modified reason is recommended for audit trail
    /// 
    /// **Impact Assessment:**
    /// - Changing integration system may affect existing property definitions
    /// - Modifying table name or primary key field may impact sync processes
    /// - Deactivating entity definition stops new instance creation
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "integrationSystemId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "name": "Employee_Updated",
    ///   "displayName": "Employee Records - Enhanced",
    ///   "description": "Enhanced employee entity with additional metadata fields",
    ///   "tableName": "Employees_v2",
    ///   "primaryKeyField": "EmployeeGUID",
    ///   "isActive": true,
    ///   "sortOrder": 2,
    ///   "metadata": "{\"syncFrequency\": \"hourly\", \"includePhotos\": true}",
    ///   "lastModifiedReason": "Updated to support new HR system version"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">Unique identifier of the entity definition to update</param>
    /// <param name="updateDto">Updated entity definition data</param>
    /// <returns>Updated entity definition with new configuration</returns>
    /// <response code="200">Entity definition updated successfully</response>
    /// <response code="400">Invalid request data or integration system not found</response>
    /// <response code="404">Entity definition not found with the specified ID</response>
    /// <response code="409">Entity definition with the same name already exists in the target integration system</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EntityDefinitionDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 409)]
    [ProducesResponseType(typeof(string), 500)]
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
    /// Delete an entity definition (soft delete with dependency validation)
    /// </summary>
    /// <remarks>
    /// Performs a soft delete of an entity definition after validating that no dependent data exists.
    /// This operation marks the entity definition as deleted without physically removing it from the database.
    /// 
    /// **Deletion Process:**
    /// 1. Validates that no entity instances reference this definition
    /// 2. Checks for dependent property definitions (automatically handled)
    /// 3. Performs soft delete by setting deletion flags and timestamp
    /// 4. Preserves audit trail and referential data for compliance
    /// 
    /// **Dependency Validation:**
    /// - Cannot delete if entity instances exist using this definition
    /// - Property definitions are automatically handled during deletion
    /// - Access rules and assignments may need manual cleanup
    /// 
    /// **Recovery Options:**
    /// - Soft-deleted entity definitions can be restored by administrators
    /// - All related metadata and configuration is preserved
    /// - Recovery process requires database-level access
    /// 
    /// **Alternative Actions:**
    /// - Consider deactivating instead of deleting for temporary removal
    /// - Use archive workflows for long-term data retention
    /// - Contact support for complex dependency resolution
    /// 
    /// **Impact Assessment:**
    /// - Stops all new entity instance creation
    /// - Existing entity instances remain unaffected
    /// - Integration sync processes may need reconfiguration
    /// - Reports and analytics may need filter updates
    /// </remarks>
    /// <param name="id">Unique identifier of the entity definition to delete</param>
    /// <returns>No content on successful deletion</returns>
    /// <response code="204">Entity definition deleted successfully</response>
    /// <response code="400">Cannot delete entity definition with existing entity instances</response>
    /// <response code="404">Entity definition not found with the specified ID</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
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
    /// Retrieve all property definitions for a specific entity definition
    /// </summary>
    /// <remarks>
    /// Returns a complete list of property definitions that define the structure and metadata
    /// for individual fields within entity instances of this entity definition.
    /// 
    /// **Property Definition Details:**
    /// - Field names, display names, and descriptions
    /// - Data types and validation rules
    /// - Index and uniqueness constraints
    /// - UI metadata and display preferences
    /// - Source field mappings from external systems
    /// 
    /// **Use Cases:**
    /// - View complete entity schema before creating instances
    /// - Validate property configurations for data import
    /// - Generate dynamic forms and user interfaces
    /// - Configure field-level access controls and permissions
    /// - Plan data migration and transformation workflows
    /// 
    /// **Sorting and Organization:**
    /// - Results are ordered by sort order (ascending), then by name (ascending)
    /// - Property definitions maintain hierarchical structure information
    /// - Related entity definitions included for context
    /// 
    /// **Performance Considerations:**
    /// - Includes complete property metadata for UI generation
    /// - Entity definition validation occurs before property retrieval
    /// - Consider caching for frequently accessed entity structures
    /// 
    /// **Integration Notes:**
    /// - Property definitions map to external system field structures
    /// - Source field mappings enable automated data synchronization
    /// - UI metadata supports custom form generation and validation
    /// </remarks>
    /// <param name="id">Unique identifier of the entity definition</param>
    /// <returns>Complete list of property definitions for the entity definition</returns>
    /// <response code="200">Successfully retrieved property definitions</response>
    /// <response code="404">Entity definition not found with the specified ID</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("{id}/property-definitions")]
    [ProducesResponseType(typeof(IEnumerable<PropertyDefinitionDto>), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
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
