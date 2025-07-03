using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Services;

public sealed class AuditService : IAuditService
{
    private readonly RbacDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditService(RbacDbContext context, ILogger<AuditService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        if (auditLog == null)
            throw new ArgumentNullException(nameof(auditLog));

        try
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Audit log created: EntityType={EntityType}, EntityId={EntityId}, Action={Action}, UserId={UserId}, CorrelationId={CorrelationId}",
                auditLog.EntityType, auditLog.EntityId, auditLog.Action, auditLog.UserId, auditLog.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to create audit log: EntityType={EntityType}, EntityId={EntityId}, Action={Action}",
                auditLog.EntityType, auditLog.EntityId, auditLog.Action);
            throw;
        }
    }

    public async Task LogEntityChangeAsync(
        string entityType,
        string entityId,
        AuditAction action,
        string userId,
        string correlationId,
        object? oldValues = null,
        object? newValues = null,
        string? justification = null,
        CancellationToken cancellationToken = default)
    {
        var oldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues, _jsonOptions) : null;
        var newValuesJson = newValues != null ? JsonSerializer.Serialize(newValues, _jsonOptions) : null;

        var auditLog = AuditLog.Create(
            entityType,
            entityId,
            action,
            userId,
            correlationId,
            oldValuesJson,
            newValuesJson,
            justification);

        await LogAsync(auditLog, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(
        string entityType,
        string entityId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type cannot be empty", nameof(entityType));
        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("Entity ID cannot be empty", nameof(entityId));

        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetUserActivityAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting user activity for UserId: {UserId}, DateRange: {FromDate} - {ToDate}, Page: {PageNumber}, Size: {PageSize}",
                userId, fromDate, toDate, pageNumber, pageSize);

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var query = _context.AuditLogs.Where(a => a.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedAt <= toDate.Value);

            var result = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} audit entries for user {UserId}", result.Count, userId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> SearchAsync(
        string? entityType = null,
        AuditAction? action = null,
        string? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? correlationId = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (action.HasValue)
            query = query.Where(a => a.Action == action.Value);

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(a => a.CorrelationId == correlationId);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}