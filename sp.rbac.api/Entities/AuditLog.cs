namespace SP.RBAC.API.Entities;

public sealed class AuditLog : BaseEntity
{
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public AuditAction Action { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string? UserName { get; private set; }
    public string? Justification { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? RequestPath { get; private set; }
    public string? RequestMethod { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public long? ExecutionTimeMs { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string entityType,
        string entityId,
        AuditAction action,
        string userId,
        string correlationId,
        string? oldValues = null,
        string? newValues = null,
        string? justification = null,
        string? userName = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestPath = null,
        string? requestMethod = null,
        int? responseStatusCode = null,
        long? executionTimeMs = null)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type cannot be empty", nameof(entityType));
        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("Entity ID cannot be empty", nameof(entityId));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be empty", nameof(correlationId));

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = userId,
            UserName = userName,
            Justification = justification,
            CorrelationId = correlationId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestPath = requestPath,
            RequestMethod = requestMethod,
            ResponseStatusCode = responseStatusCode,
            ExecutionTimeMs = executionTimeMs,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
    }

    public void UpdateExecutionMetrics(int statusCode, long executionTime)
    {
        ResponseStatusCode = statusCode;
        ExecutionTimeMs = executionTime;
    }

    /// <summary>
    /// Set HTTP context information for the audit log
    /// </summary>
    public void SetHttpContext(
        string? ipAddress = null,
        string? userAgent = null,
        string? requestPath = null,
        string? requestMethod = null,
        int? responseStatusCode = null,
        long? executionTimeMs = null)
    {
        if (!string.IsNullOrEmpty(ipAddress))
            IpAddress = ipAddress;
        
        if (!string.IsNullOrEmpty(userAgent))
            UserAgent = userAgent;
        
        if (!string.IsNullOrEmpty(requestPath))
            RequestPath = requestPath;
        
        if (!string.IsNullOrEmpty(requestMethod))
            RequestMethod = requestMethod;
        
        if (responseStatusCode.HasValue)
            ResponseStatusCode = responseStatusCode.Value;
        
        if (executionTimeMs.HasValue)
            ExecutionTimeMs = executionTimeMs.Value;
    }

    /// <summary>
    /// Set user information for the audit log
    /// </summary>
    public void SetUserInfo(string userId, string? userName = null)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        UserName = userName;
    }

    /// <summary>
    /// Set entity change information
    /// </summary>
    public void SetEntityChange(string? oldValues = null, string? newValues = null)
    {
        OldValues = oldValues;
        NewValues = newValues;
    }

    /// <summary>
    /// Set justification for the audit action
    /// </summary>
    public void SetJustification(string justification)
    {
        Justification = justification;
    }
}
