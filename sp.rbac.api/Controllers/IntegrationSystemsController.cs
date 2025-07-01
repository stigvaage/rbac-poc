using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Get all integration systems with pagination
    /// </summary>
    [HttpGet]
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
    /// Get a specific integration system by ID
    /// </summary>
    [HttpGet("{id}")]
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
