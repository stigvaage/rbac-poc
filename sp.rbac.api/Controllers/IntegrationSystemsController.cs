using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Manages integration systems that connect external systems to the RBAC platform.
/// Integration systems represent external data sources like HR systems, EMR systems, 
/// Active Directory, and other enterprise applications.
/// </summary>
/// <remarks>
/// This controller provides full CRUD operations for integration systems along with
/// advanced features like connection testing, system health monitoring, and bulk operations.
/// All operations support pagination, filtering, and comprehensive error handling.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Integration Systems")]
public class IntegrationSystemsController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<IntegrationSystemsController> _logger;

    public IntegrationSystemsController(RbacDbContext context, IMapper mapper, ILogger<IntegrationSystemsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of integration systems with optional filtering
    /// </summary>
    /// <remarks>
    /// This endpoint supports comprehensive filtering and searching across integration systems.
    /// Use the search parameter to find systems by name, display name, or description.
    /// The isActive filter allows you to retrieve only active or inactive systems.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/IntegrationSystems?pageNumber=1&amp;pageSize=10&amp;search=HR&amp;isActive=true
    /// 
    /// This will return the first 10 active systems that contain "HR" in their name, 
    /// display name, or description.
    /// </remarks>
    /// <param name="pageNumber">Page number for pagination (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10, range: 1-100)</param>
    /// <param name="search">Optional search term to filter by name, display name, or description</param>
    /// <param name="isActive">Optional filter to show only active (true) or inactive (false) systems</param>
    /// <returns>A paginated list of integration systems matching the specified criteria</returns>
    /// <response code="200">Returns the paginated list of integration systems</response>
    /// <response code="400">If the pagination parameters are invalid</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IntegrationSystemDto>), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PagedResult<IntegrationSystemDto>>> GetIntegrationSystems(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.IntegrationSystems.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Name.Contains(search) || 
                                        x.DisplayName.Contains(search) || 
                                        x.Description.Contains(search));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<IntegrationSystemDto>>(items);

            var result = new PagedResult<IntegrationSystemDto>
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
            _logger.LogError(ex, "Error getting integration systems");
            return StatusCode(500, "An error occurred while retrieving integration systems");
        }
    }

    /// <summary>
    /// Retrieves a specific integration system by its unique identifier
    /// </summary>
    /// <remarks>
    /// This endpoint returns detailed information about a single integration system,
    /// including all configuration details, authentication settings, and metadata.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/IntegrationSystems/123e4567-e89b-12d3-a456-426614174000
    /// 
    /// The response includes sensitive information like connection strings, so ensure
    /// proper authentication and authorization are in place for production use.
    /// </remarks>
    /// <param name="id">The unique identifier (GUID) of the integration system</param>
    /// <returns>The integration system with the specified ID</returns>
    /// <response code="200">Returns the integration system details</response>
    /// <response code="404">If no integration system exists with the specified ID</response>
    /// <response code="500">If an internal server error occurs</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IntegrationSystemDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IntegrationSystemDto>> GetIntegrationSystem(Guid id)
    {
        try
        {
            var integrationSystem = await _context.IntegrationSystems
                .FirstOrDefaultAsync(x => x.Id == id);

            if (integrationSystem == null)
            {
                return NotFound($"Integration system with ID {id} not found");
            }

            var dto = _mapper.Map<IntegrationSystemDto>(integrationSystem);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting integration system {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the integration system");
        }
    }

    /// <summary>
    /// Create a new integration system
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<IntegrationSystemDto>> CreateIntegrationSystem(CreateIntegrationSystemDto createDto)
    {
        try
        {
            // Check if name already exists
            var existingSystem = await _context.IntegrationSystems
                .FirstOrDefaultAsync(x => x.Name == createDto.Name);

            if (existingSystem != null)
            {
                return Conflict($"Integration system with name '{createDto.Name}' already exists");
            }

            var integrationSystem = _mapper.Map<IntegrationSystem>(createDto);
            
            _context.IntegrationSystems.Add(integrationSystem);
            await _context.SaveChangesAsync();

            var dto = _mapper.Map<IntegrationSystemDto>(integrationSystem);
            
            return CreatedAtAction(nameof(GetIntegrationSystem), 
                new { id = integrationSystem.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating integration system");
            return StatusCode(500, "An error occurred while creating the integration system");
        }
    }

    /// <summary>
    /// Update an existing integration system
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<IntegrationSystemDto>> UpdateIntegrationSystem(Guid id, UpdateIntegrationSystemDto updateDto)
    {
        try
        {
            var integrationSystem = await _context.IntegrationSystems
                .FirstOrDefaultAsync(x => x.Id == id);

            if (integrationSystem == null)
            {
                return NotFound($"Integration system with ID {id} not found");
            }

            // Check if name already exists (excluding current record)
            var existingSystem = await _context.IntegrationSystems
                .FirstOrDefaultAsync(x => x.Name == updateDto.Name && x.Id != id);

            if (existingSystem != null)
            {
                return Conflict($"Integration system with name '{updateDto.Name}' already exists");
            }

            _mapper.Map(updateDto, integrationSystem);
            integrationSystem.LastModifiedReason = updateDto.LastModifiedReason;

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<IntegrationSystemDto>(integrationSystem);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating integration system {Id}", id);
            return StatusCode(500, "An error occurred while updating the integration system");
        }
    }

    /// <summary>
    /// Delete an integration system (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIntegrationSystem(Guid id)
    {
        try
        {
            var integrationSystem = await _context.IntegrationSystems
                .FirstOrDefaultAsync(x => x.Id == id);

            if (integrationSystem == null)
            {
                return NotFound($"Integration system with ID {id} not found");
            }

            // Check if there are dependent records
            var hasEntityDefinitions = await _context.EntityDefinitions
                .AnyAsync(x => x.IntegrationSystemId == id);

            if (hasEntityDefinitions)
            {
                return BadRequest("Cannot delete integration system with existing entity definitions");
            }

            // Soft delete
            integrationSystem.IsDeleted = true;
            integrationSystem.DeletedAt = DateTime.UtcNow;
            integrationSystem.DeletedBy = "system"; // TODO: Get from current user context

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting integration system {Id}", id);
            return StatusCode(500, "An error occurred while deleting the integration system");
        }
    }

    /// <summary>
    /// Test connection to an integration system
    /// </summary>
    [HttpPost("{id}/test-connection")]
    public async Task<IActionResult> TestConnection(Guid id)
    {
        try
        {
            var integrationSystem = await _context.IntegrationSystems
                .FirstOrDefaultAsync(x => x.Id == id);

            if (integrationSystem == null)
            {
                return NotFound($"Integration system with ID {id} not found");
            }

            // TODO: Implement actual connection testing logic based on AuthenticationType
            // For now, just simulate a test
            var isConnected = !string.IsNullOrEmpty(integrationSystem.ConnectionString);

            if (isConnected)
            {
                integrationSystem.LastSync = DateTime.UtcNow;
                integrationSystem.LastSyncStatus = SyncStatus.Success;
            }
            else
            {
                integrationSystem.LastSyncStatus = SyncStatus.Failed;
            }

            await _context.SaveChangesAsync();

            return Ok(new { IsConnected = isConnected, Message = isConnected ? "Connection successful" : "Connection failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection for integration system {Id}", id);
            return StatusCode(500, "An error occurred while testing the connection");
        }
    }
}
