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
public class AccessAssignmentsController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<AccessAssignmentsController> _logger;

    public AccessAssignmentsController(RbacDbContext context, IMapper mapper, ILogger<AccessAssignmentsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all access assignments with optional filtering and pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="roleId">Filter by role ID</param>
    /// <param name="targetSystemId">Filter by target system ID</param>
    /// <param name="assignmentType">Filter by assignment type</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="search">Search in assignment reason and metadata</param>
    /// <returns>Paginated list of access assignments</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AccessAssignmentDto>>> GetAccessAssignments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? roleId = null,
        [FromQuery] Guid? targetSystemId = null,
        [FromQuery] AssignmentType? assignmentType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .AsQueryable();

            // Apply filters
            if (userId.HasValue)
                query = query.Where(aa => aa.UserId == userId.Value);

            if (roleId.HasValue)
                query = query.Where(aa => aa.RoleId == roleId.Value);

            if (targetSystemId.HasValue)
                query = query.Where(aa => aa.TargetSystemId == targetSystemId.Value);

            if (assignmentType.HasValue)
                query = query.Where(aa => aa.AssignmentType == assignmentType.Value);

            if (isActive.HasValue)
                query = query.Where(aa => aa.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(aa => 
                    (aa.AssignmentReason != null && aa.AssignmentReason.ToLower().Contains(searchLower)) ||
                    aa.Metadata.ToLower().Contains(searchLower) ||
                    aa.User.DisplayName.ToLower().Contains(searchLower) ||
                    aa.Role.DisplayName.ToLower().Contains(searchLower) ||
                    aa.TargetSystem.Name.ToLower().Contains(searchLower));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(aa => aa.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<AccessAssignmentDto>>(items);

            var result = new PagedResult<AccessAssignmentDto>
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
            _logger.LogError(ex, "Error retrieving access assignments");
            return StatusCode(500, "An error occurred while retrieving access assignments");
        }
    }

    /// <summary>
    /// Get access assignment by ID
    /// </summary>
    /// <param name="id">Access assignment ID</param>
    /// <returns>Access assignment details</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccessAssignmentDto>> GetAccessAssignment(Guid id)
    {
        try
        {
            var accessAssignment = await _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .FirstOrDefaultAsync(aa => aa.Id == id);

            if (accessAssignment == null)
                return NotFound($"Access assignment with ID {id} not found");

            var dto = _mapper.Map<AccessAssignmentDto>(accessAssignment);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access assignment {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the access assignment");
        }
    }

    /// <summary>
    /// Create a new access assignment
    /// </summary>
    /// <param name="dto">Access assignment creation data</param>
    /// <returns>Created access assignment</returns>
    [HttpPost]
    public async Task<ActionResult<AccessAssignmentDto>> CreateAccessAssignment(CreateAccessAssignmentDto dto)
    {
        try
        {
            // Validate referenced entities exist
            var userExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.UserId);
            if (!userExists)
                return BadRequest($"User with ID {dto.UserId} not found");

            var roleExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.RoleId);
            if (!roleExists)
                return BadRequest($"Role with ID {dto.RoleId} not found");

            var targetSystemExists = await _context.IntegrationSystems.AnyAsync(is_ => is_.Id == dto.TargetSystemId);
            if (!targetSystemExists)
                return BadRequest($"Target system with ID {dto.TargetSystemId} not found");

            // Check for existing active assignment
            var existingAssignment = await _context.AccessAssignments
                .FirstOrDefaultAsync(aa => 
                    aa.UserId == dto.UserId && 
                    aa.RoleId == dto.RoleId && 
                    aa.TargetSystemId == dto.TargetSystemId &&
                    aa.IsActive);

            if (existingAssignment != null)
                return Conflict("An active access assignment already exists for this user-role-system combination");

            var accessAssignment = _mapper.Map<AccessAssignment>(dto);
            accessAssignment.Id = Guid.NewGuid();
            accessAssignment.CreatedAt = DateTime.UtcNow;
            accessAssignment.CreatedBy = "System"; // In real app, get from user context

            _context.AccessAssignments.Add(accessAssignment);
            await _context.SaveChangesAsync();

            // Fetch the created assignment with navigation properties
            var createdAssignment = await _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .FirstOrDefaultAsync(aa => aa.Id == accessAssignment.Id);

            var resultDto = _mapper.Map<AccessAssignmentDto>(createdAssignment);
            return CreatedAtAction(nameof(GetAccessAssignment), new { id = accessAssignment.Id }, resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating access assignment");
            return StatusCode(500, "An error occurred while creating the access assignment");
        }
    }

    /// <summary>
    /// Update an existing access assignment
    /// </summary>
    /// <param name="id">Access assignment ID</param>
    /// <param name="dto">Access assignment update data</param>
    /// <returns>Updated access assignment</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccessAssignmentDto>> UpdateAccessAssignment(Guid id, UpdateAccessAssignmentDto dto)
    {
        try
        {
            var existingAssignment = await _context.AccessAssignments.FindAsync(id);
            if (existingAssignment == null)
                return NotFound($"Access assignment with ID {id} not found");

            // Validate referenced entities exist
            var userExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.UserId);
            if (!userExists)
                return BadRequest($"User with ID {dto.UserId} not found");

            var roleExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.RoleId);
            if (!roleExists)
                return BadRequest($"Role with ID {dto.RoleId} not found");

            var targetSystemExists = await _context.IntegrationSystems.AnyAsync(is_ => is_.Id == dto.TargetSystemId);
            if (!targetSystemExists)
                return BadRequest($"Target system with ID {dto.TargetSystemId} not found");

            // Check for conflicting active assignment (if changing key fields)
            if (existingAssignment.UserId != dto.UserId || 
                existingAssignment.RoleId != dto.RoleId || 
                existingAssignment.TargetSystemId != dto.TargetSystemId)
            {
                var conflictingAssignment = await _context.AccessAssignments
                    .FirstOrDefaultAsync(aa => 
                        aa.Id != id &&
                        aa.UserId == dto.UserId && 
                        aa.RoleId == dto.RoleId && 
                        aa.TargetSystemId == dto.TargetSystemId &&
                        aa.IsActive);

                if (conflictingAssignment != null)
                    return Conflict("An active access assignment already exists for this user-role-system combination");
            }

            _mapper.Map(dto, existingAssignment);
            existingAssignment.UpdatedAt = DateTime.UtcNow;
            existingAssignment.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            // Fetch updated assignment with navigation properties
            var updatedAssignment = await _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .FirstOrDefaultAsync(aa => aa.Id == id);

            var resultDto = _mapper.Map<AccessAssignmentDto>(updatedAssignment);
            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating access assignment {Id}", id);
            return StatusCode(500, "An error occurred while updating the access assignment");
        }
    }

    /// <summary>
    /// Delete an access assignment
    /// </summary>
    /// <param name="id">Access assignment ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAccessAssignment(Guid id)
    {
        try
        {
            var accessAssignment = await _context.AccessAssignments.FindAsync(id);
            if (accessAssignment == null)
                return NotFound($"Access assignment with ID {id} not found");

            // Soft delete
            accessAssignment.IsDeleted = true;
            accessAssignment.DeletedAt = DateTime.UtcNow;
            accessAssignment.DeletedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting access assignment {Id}", id);
            return StatusCode(500, "An error occurred while deleting the access assignment");
        }
    }

    /// <summary>
    /// Get access assignments for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeInactive">Include inactive assignments</param>
    /// <returns>List of user's access assignments</returns>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<AccessAssignmentDto>>> GetUserAccessAssignments(
        Guid userId, 
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .Where(aa => aa.UserId == userId);

            if (!includeInactive)
                query = query.Where(aa => aa.IsActive);

            var assignments = await query
                .OrderByDescending(aa => aa.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<AccessAssignmentDto>>(assignments);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access assignments for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving access assignments");
        }
    }

    /// <summary>
    /// Get access assignments for a specific target system
    /// </summary>
    /// <param name="targetSystemId">Target system ID</param>
    /// <param name="includeInactive">Include inactive assignments</param>
    /// <returns>List of target system's access assignments</returns>
    [HttpGet("system/{targetSystemId:guid}")]
    public async Task<ActionResult<List<AccessAssignmentDto>>> GetSystemAccessAssignments(
        Guid targetSystemId, 
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .Where(aa => aa.TargetSystemId == targetSystemId);

            if (!includeInactive)
                query = query.Where(aa => aa.IsActive);

            var assignments = await query
                .OrderByDescending(aa => aa.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<AccessAssignmentDto>>(assignments);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access assignments for system {SystemId}", targetSystemId);
            return StatusCode(500, "An error occurred while retrieving access assignments");
        }
    }

    /// <summary>
    /// Get available assignment types enum values
    /// </summary>
    /// <returns>List of assignment type values</returns>
    [HttpGet("assignment-types")]
    public ActionResult<Dictionary<string, int>> GetAssignmentTypes()
    {
        var assignmentTypes = Enum.GetValues<AssignmentType>()
            .ToDictionary(at => at.ToString(), at => (int)at);
        
        return Ok(assignmentTypes);
    }

    /// <summary>
    /// Activate/deactivate an access assignment
    /// </summary>
    /// <param name="id">Access assignment ID</param>
    /// <param name="isActive">Active status</param>
    /// <returns>Updated access assignment</returns>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<AccessAssignmentDto>> UpdateAssignmentStatus(Guid id, [FromBody] bool isActive)
    {
        try
        {
            var assignment = await _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .FirstOrDefaultAsync(aa => aa.Id == id);

            if (assignment == null)
                return NotFound($"Access assignment with ID {id} not found");

            assignment.IsActive = isActive;
            assignment.UpdatedAt = DateTime.UtcNow;
            assignment.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<AccessAssignmentDto>(assignment);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment status {Id}", id);
            return StatusCode(500, "An error occurred while updating the assignment status");
        }
    }
}
