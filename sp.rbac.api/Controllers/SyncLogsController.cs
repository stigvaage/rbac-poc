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
public class SyncLogsController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SyncLogsController> _logger;

    public SyncLogsController(RbacDbContext context, IMapper mapper, ILogger<SyncLogsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all sync logs with optional filtering and pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="integrationSystemId">Filter by integration system ID</param>
    /// <param name="operation">Filter by operation type</param>
    /// <param name="status">Filter by sync status</param>
    /// <param name="startDate">Filter by start date (from)</param>
    /// <param name="endDate">Filter by start date (to)</param>
    /// <param name="search">Search in operation and error message</param>
    /// <returns>Paginated list of sync logs</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<SyncLogDto>>> GetSyncLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? integrationSystemId = null,
        [FromQuery] string? operation = null,
        [FromQuery] SyncStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.SyncLogs
                .Include(sl => sl.IntegrationSystem)
                .AsQueryable();

            // Apply filters
            if (integrationSystemId.HasValue)
                query = query.Where(sl => sl.IntegrationSystemId == integrationSystemId.Value);

            if (!string.IsNullOrWhiteSpace(operation))
                query = query.Where(sl => sl.Operation.ToLower().Contains(operation.ToLower()));

            if (status.HasValue)
                query = query.Where(sl => sl.Status == status.Value);

            if (startDate.HasValue)
                query = query.Where(sl => sl.StartedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(sl => sl.StartedAt <= endDate.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(sl => 
                    sl.Operation.ToLower().Contains(searchLower) ||
                    (sl.ErrorMessage != null && sl.ErrorMessage.ToLower().Contains(searchLower)) ||
                    sl.IntegrationSystem.Name.ToLower().Contains(searchLower));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(sl => sl.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<SyncLogDto>>(items);

            var result = new PagedResult<SyncLogDto>
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
            _logger.LogError(ex, "Error retrieving sync logs");
            return StatusCode(500, "An error occurred while retrieving sync logs");
        }
    }

    /// <summary>
    /// Get sync log by ID
    /// </summary>
    /// <param name="id">Sync log ID</param>
    /// <returns>Sync log details</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SyncLogDto>> GetSyncLog(Guid id)
    {
        try
        {
            var syncLog = await _context.SyncLogs
                .Include(sl => sl.IntegrationSystem)
                .FirstOrDefaultAsync(sl => sl.Id == id);

            if (syncLog == null)
                return NotFound($"Sync log with ID {id} not found");

            var dto = _mapper.Map<SyncLogDto>(syncLog);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sync log {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the sync log");
        }
    }

    /// <summary>
    /// Create a new sync log
    /// </summary>
    /// <param name="dto">Sync log creation data</param>
    /// <returns>Created sync log</returns>
    [HttpPost]
    public async Task<ActionResult<SyncLogDto>> CreateSyncLog(CreateSyncLogDto dto)
    {
        try
        {
            // Validate referenced integration system exists
            var integrationSystemExists = await _context.IntegrationSystems.AnyAsync(is_ => is_.Id == dto.IntegrationSystemId);
            if (!integrationSystemExists)
                return BadRequest($"Integration system with ID {dto.IntegrationSystemId} not found");

            var syncLog = _mapper.Map<SyncLog>(dto);
            syncLog.Id = Guid.NewGuid();
            syncLog.CreatedAt = DateTime.UtcNow;
            syncLog.CreatedBy = "System"; // In real app, get from user context

            _context.SyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();

            // Fetch the created sync log with navigation properties
            var createdSyncLog = await _context.SyncLogs
                .Include(sl => sl.IntegrationSystem)
                .FirstOrDefaultAsync(sl => sl.Id == syncLog.Id);

            var resultDto = _mapper.Map<SyncLogDto>(createdSyncLog);
            return CreatedAtAction(nameof(GetSyncLog), new { id = syncLog.Id }, resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sync log");
            return StatusCode(500, "An error occurred while creating the sync log");
        }
    }

    /// <summary>
    /// Update an existing sync log
    /// </summary>
    /// <param name="id">Sync log ID</param>
    /// <param name="dto">Sync log update data</param>
    /// <returns>Updated sync log</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SyncLogDto>> UpdateSyncLog(Guid id, UpdateSyncLogDto dto)
    {
        try
        {
            var existingSyncLog = await _context.SyncLogs.FindAsync(id);
            if (existingSyncLog == null)
                return NotFound($"Sync log with ID {id} not found");

            _mapper.Map(dto, existingSyncLog);
            existingSyncLog.UpdatedAt = DateTime.UtcNow;
            existingSyncLog.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            // Fetch updated sync log with navigation properties
            var updatedSyncLog = await _context.SyncLogs
                .Include(sl => sl.IntegrationSystem)
                .FirstOrDefaultAsync(sl => sl.Id == id);

            var resultDto = _mapper.Map<SyncLogDto>(updatedSyncLog);
            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sync log {Id}", id);
            return StatusCode(500, "An error occurred while updating the sync log");
        }
    }

    /// <summary>
    /// Delete a sync log
    /// </summary>
    /// <param name="id">Sync log ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteSyncLog(Guid id)
    {
        try
        {
            var syncLog = await _context.SyncLogs.FindAsync(id);
            if (syncLog == null)
                return NotFound($"Sync log with ID {id} not found");

            // Hard delete for sync logs (they're typically audit records)
            _context.SyncLogs.Remove(syncLog);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sync log {Id}", id);
            return StatusCode(500, "An error occurred while deleting the sync log");
        }
    }

    /// <summary>
    /// Get sync logs for a specific integration system
    /// </summary>
    /// <param name="integrationSystemId">Integration system ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="status">Filter by sync status</param>
    /// <returns>List of integration system's sync logs</returns>
    [HttpGet("integration-system/{integrationSystemId:guid}")]
    public async Task<ActionResult<PagedResult<SyncLogDto>>> GetIntegrationSystemSyncLogs(
        Guid integrationSystemId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] SyncStatus? status = null)
    {
        try
        {
            var query = _context.SyncLogs
                .Include(sl => sl.IntegrationSystem)
                .Where(sl => sl.IntegrationSystemId == integrationSystemId);

            if (status.HasValue)
                query = query.Where(sl => sl.Status == status.Value);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(sl => sl.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<SyncLogDto>>(items);

            var result = new PagedResult<SyncLogDto>
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
            _logger.LogError(ex, "Error retrieving sync logs for integration system {SystemId}", integrationSystemId);
            return StatusCode(500, "An error occurred while retrieving sync logs");
        }
    }

    /// <summary>
    /// Get sync status enum values
    /// </summary>
    /// <returns>List of sync status values</returns>
    [HttpGet("sync-statuses")]
    public ActionResult<Dictionary<string, int>> GetSyncStatuses()
    {
        var syncStatuses = Enum.GetValues<SyncStatus>()
            .ToDictionary(ss => ss.ToString(), ss => (int)ss);
        
        return Ok(syncStatuses);
    }

    /// <summary>
    /// Get sync log summary statistics
    /// </summary>
    /// <param name="integrationSystemId">Optional filter by integration system</param>
    /// <param name="days">Number of days to look back (default 30)</param>
    /// <returns>Sync log summary statistics</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<List<SyncLogSummaryDto>>> GetSyncLogSummary(
        [FromQuery] Guid? integrationSystemId = null,
        [FromQuery] int days = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            
            var query = _context.SyncLogs
                .Include(sl => sl.IntegrationSystem)
                .Where(sl => sl.StartedAt >= cutoffDate);

            if (integrationSystemId.HasValue)
                query = query.Where(sl => sl.IntegrationSystemId == integrationSystemId.Value);

            var syncLogs = await query.ToListAsync();

            var summary = syncLogs
                .GroupBy(sl => new { sl.IntegrationSystemId, sl.IntegrationSystem.Name })
                .Select(g => new SyncLogSummaryDto
                {
                    IntegrationSystemId = g.Key.IntegrationSystemId,
                    IntegrationSystemName = g.Key.Name,
                    TotalSyncs = g.Count(),
                    SuccessfulSyncs = g.Count(sl => sl.Status == SyncStatus.Success),
                    FailedSyncs = g.Count(sl => sl.Status == SyncStatus.Failed),
                    LastSyncDate = g.Max(sl => sl.StartedAt),
                    LastSyncStatus = g.OrderByDescending(sl => sl.StartedAt).FirstOrDefault()?.Status
                })
                .OrderByDescending(s => s.LastSyncDate)
                .ToList();

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sync log summary");
            return StatusCode(500, "An error occurred while retrieving sync log summary");
        }
    }

    /// <summary>
    /// Complete a sync log operation
    /// </summary>
    /// <param name="id">Sync log ID</param>
    /// <param name="dto">Completion data</param>
    /// <returns>Updated sync log</returns>
    [HttpPatch("{id:guid}/complete")]
    public async Task<ActionResult<SyncLogDto>> CompleteSyncLog(Guid id, [FromBody] UpdateSyncLogDto dto)
    {
        try
        {
            var syncLog = await _context.SyncLogs
                .Include(sl => sl.IntegrationSystem)
                .FirstOrDefaultAsync(sl => sl.Id == id);

            if (syncLog == null)
                return NotFound($"Sync log with ID {id} not found");

            // Update completion data
            _mapper.Map(dto, syncLog);
            syncLog.CompletedAt = DateTime.UtcNow;
            syncLog.UpdatedAt = DateTime.UtcNow;
            syncLog.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<SyncLogDto>(syncLog);
            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing sync log {Id}", id);
            return StatusCode(500, "An error occurred while completing the sync log");
        }
    }

    /// <summary>
    /// Get recent failed sync logs
    /// </summary>
    /// <param name="hours">Number of hours to look back (default 24)</param>
    /// <param name="limit">Maximum number of records to return (default 50)</param>
    /// <returns>List of recent failed sync logs</returns>
    [HttpGet("failed")]
    public async Task<ActionResult<List<SyncLogDto>>> GetRecentFailedSyncLogs(
        [FromQuery] int hours = 24,
        [FromQuery] int limit = 50)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddHours(-hours);
            
            var failedSyncLogs = await _context.SyncLogs
                .Include(sl => sl.IntegrationSystem)
                .Where(sl => sl.StartedAt >= cutoffDate && sl.Status == SyncStatus.Failed)
                .OrderByDescending(sl => sl.StartedAt)
                .Take(limit)
                .ToListAsync();

            var dtos = _mapper.Map<List<SyncLogDto>>(failedSyncLogs);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent failed sync logs");
            return StatusCode(500, "An error occurred while retrieving recent failed sync logs");
        }
    }
}
