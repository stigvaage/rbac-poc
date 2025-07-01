using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessRulesController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<AccessRulesController> _logger;

    public AccessRulesController(RbacDbContext context, IMapper mapper, ILogger<AccessRulesController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all access rules with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AccessRuleDto>>> GetAccessRules(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? integrationSystemId = null,
        [FromQuery] TriggerType? triggerType = null,
        [FromQuery] ActionType? actionType = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.AccessRules
                .Include(x => x.IntegrationSystem)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Name.Contains(search) || 
                                        x.Description.Contains(search));
            }

            if (integrationSystemId.HasValue)
            {
                query = query.Where(x => x.IntegrationSystemId == integrationSystemId.Value);
            }

            if (triggerType.HasValue)
            {
                query = query.Where(x => x.TriggerType == triggerType.Value);
            }

            if (actionType.HasValue)
            {
                query = query.Where(x => x.ActionType == actionType.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<AccessRuleDto>>(items);

            var result = new PagedResult<AccessRuleDto>
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
            _logger.LogError(ex, "Error getting access rules");
            return StatusCode(500, "An error occurred while retrieving access rules");
        }
    }

    /// <summary>
    /// Get a specific access rule by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AccessRuleDto>> GetAccessRule(Guid id)
    {
        try
        {
            var accessRule = await _context.AccessRules
                .Include(x => x.IntegrationSystem)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (accessRule == null)
            {
                return NotFound($"Access rule with ID {id} not found");
            }

            var dto = _mapper.Map<AccessRuleDto>(accessRule);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access rule {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the access rule");
        }
    }

    /// <summary>
    /// Create a new access rule
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AccessRuleDto>> CreateAccessRule(CreateAccessRuleDto createDto)
    {
        try
        {
            // Check if integration system exists (if specified)
            if (createDto.IntegrationSystemId.HasValue)
            {
                var integrationSystemExists = await _context.IntegrationSystems
                    .AnyAsync(x => x.Id == createDto.IntegrationSystemId.Value);

                if (!integrationSystemExists)
                {
                    return BadRequest($"Integration system with ID {createDto.IntegrationSystemId} not found");
                }
            }

            // Check if name already exists
            var existingRule = await _context.AccessRules
                .FirstOrDefaultAsync(x => x.Name == createDto.Name);

            if (existingRule != null)
            {
                return Conflict($"Access rule with name '{createDto.Name}' already exists");
            }

            var accessRule = _mapper.Map<AccessRule>(createDto);
            
            _context.AccessRules.Add(accessRule);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            accessRule = await _context.AccessRules
                .Include(x => x.IntegrationSystem)
                .FirstAsync(x => x.Id == accessRule.Id);

            var dto = _mapper.Map<AccessRuleDto>(accessRule);
            
            return CreatedAtAction(nameof(GetAccessRule), 
                new { id = accessRule.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating access rule");
            return StatusCode(500, "An error occurred while creating the access rule");
        }
    }

    /// <summary>
    /// Update an existing access rule
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AccessRuleDto>> UpdateAccessRule(Guid id, UpdateAccessRuleDto updateDto)
    {
        try
        {
            var accessRule = await _context.AccessRules
                .Include(x => x.IntegrationSystem)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (accessRule == null)
            {
                return NotFound($"Access rule with ID {id} not found");
            }

            // Check if integration system exists (if specified)
            if (updateDto.IntegrationSystemId.HasValue)
            {
                var integrationSystemExists = await _context.IntegrationSystems
                    .AnyAsync(x => x.Id == updateDto.IntegrationSystemId.Value);

                if (!integrationSystemExists)
                {
                    return BadRequest($"Integration system with ID {updateDto.IntegrationSystemId} not found");
                }
            }

            // Check if name already exists (excluding current record)
            var existingRule = await _context.AccessRules
                .FirstOrDefaultAsync(x => x.Name == updateDto.Name && x.Id != id);

            if (existingRule != null)
            {
                return Conflict($"Access rule with name '{updateDto.Name}' already exists");
            }

            _mapper.Map(updateDto, accessRule);
            accessRule.LastModifiedReason = updateDto.LastModifiedReason;

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<AccessRuleDto>(accessRule);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating access rule {Id}", id);
            return StatusCode(500, "An error occurred while updating the access rule");
        }
    }

    /// <summary>
    /// Delete an access rule (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccessRule(Guid id)
    {
        try
        {
            var accessRule = await _context.AccessRules
                .FirstOrDefaultAsync(x => x.Id == id);

            if (accessRule == null)
            {
                return NotFound($"Access rule with ID {id} not found");
            }

            // Check if there are dependent records
            var hasAccessAssignments = await _context.AccessAssignments
                .AnyAsync(x => x.AccessRules.Any(ar => ar.Id == id));

            if (hasAccessAssignments)
            {
                return BadRequest("Cannot delete access rule with existing access assignments");
            }

            // Soft delete
            accessRule.IsDeleted = true;
            accessRule.DeletedAt = DateTime.UtcNow;
            accessRule.DeletedBy = "system"; // TODO: Get from current user context

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting access rule {Id}", id);
            return StatusCode(500, "An error occurred while deleting the access rule");
        }
    }

    /// <summary>
    /// Execute an access rule manually
    /// </summary>
    [HttpPost("{id}/execute")]
    public async Task<IActionResult> ExecuteAccessRule(Guid id)
    {
        try
        {
            var accessRule = await _context.AccessRules
                .FirstOrDefaultAsync(x => x.Id == id);

            if (accessRule == null)
            {
                return NotFound($"Access rule with ID {id} not found");
            }

            if (!accessRule.IsActive)
            {
                return BadRequest("Cannot execute inactive access rule");
            }

            // TODO: Implement actual rule execution logic
            // For now, just update the execution timestamp
            accessRule.LastExecuted = DateTime.UtcNow;
            accessRule.LastExecutionResult = "Executed manually - implementation pending";

            await _context.SaveChangesAsync();

            return Ok(new { 
                Message = "Access rule executed successfully", 
                ExecutedAt = accessRule.LastExecuted,
                Result = accessRule.LastExecutionResult 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing access rule {Id}", id);
            return StatusCode(500, "An error occurred while executing the access rule");
        }
    }

    /// <summary>
    /// Get available trigger types
    /// </summary>
    [HttpGet("trigger-types")]
    public ActionResult<IEnumerable<object>> GetTriggerTypes()
    {
        try
        {
            var triggerTypes = Enum.GetValues<TriggerType>()
                .Select(tt => new { Value = (int)tt, Name = tt.ToString() })
                .ToList();

            return Ok(triggerTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trigger types");
            return StatusCode(500, "An error occurred while retrieving trigger types");
        }
    }

    /// <summary>
    /// Get available action types
    /// </summary>
    [HttpGet("action-types")]
    public ActionResult<IEnumerable<object>> GetActionTypes()
    {
        try
        {
            var actionTypes = Enum.GetValues<ActionType>()
                .Select(at => new { Value = (int)at, Name = at.ToString() })
                .ToList();

            return Ok(actionTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting action types");
            return StatusCode(500, "An error occurred while retrieving action types");
        }
    }
}
