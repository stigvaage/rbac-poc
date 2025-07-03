using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Services;

public interface IAuditService
{
    Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task LogEntityChangeAsync(
        string entityType,
        string entityId,
        AuditAction action,
        string userId,
        string correlationId,
        object? oldValues = null,
        object? newValues = null,
        string? justification = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(
        string entityType,
        string entityId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetUserActivityAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> SearchAsync(
        string? entityType = null,
        AuditAction? action = null,
        string? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? correlationId = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
