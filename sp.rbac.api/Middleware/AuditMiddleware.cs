using SP.RBAC.API.Entities;
using SP.RBAC.API.Services;
using System.Diagnostics;
using System.Text;

namespace SP.RBAC.API.Middleware;

/// <summary>
/// Middleware to automatically log audit information for HTTP requests
/// </summary>
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString();

        // Add correlation ID to response headers
        context.Response.Headers.TryAdd("X-Correlation-ID", correlationId);

        // Store correlation ID and start time in context for use by controllers
        context.Items["CorrelationId"] = correlationId;
        context.Items["RequestStartTime"] = DateTimeOffset.UtcNow;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Only log audit for API endpoints (not static files, swagger, etc.)
            if (ShouldAuditRequest(context))
            {
                await LogRequestAuditAsync(context, correlationId, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private static bool ShouldAuditRequest(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        if (string.IsNullOrEmpty(path))
            return false;

        // Audit all API endpoints
        if (path.StartsWith("/api/"))
            return true;

        // Don't audit these paths
        var excludePaths = new[]
        {
            "/swagger",
            "/health",
            "/favicon.ico",
            "/_framework",
            "/css",
            "/js",
            "/images"
        };

        return !excludePaths.Any(exclude => path.StartsWith(exclude));
    }

    private async Task LogRequestAuditAsync(HttpContext context, string correlationId, long executionTimeMs)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var auditService = scope.ServiceProvider.GetService<IAuditService>();
            
            if (auditService == null)
                return;

            // Extract user information (this would be improved with proper authentication)
            var userId = context.User?.Identity?.Name ?? "anonymous";
            var userName = context.User?.FindFirst("name")?.Value;

            // Determine audit action based on HTTP method
            var action = GetAuditActionFromHttpMethod(context.Request.Method);

            // Create audit log for the HTTP request
            var auditLog = AuditLog.Create(
                entityType: "HttpRequest",
                entityId: $"{context.Request.Method}:{context.Request.Path}",
                action: action,
                userId: userId,
                correlationId: correlationId);

            // Set HTTP context information
            auditLog.SetHttpContext(
                ipAddress: GetClientIpAddress(context),
                userAgent: context.Request.Headers.UserAgent.ToString(),
                requestPath: context.Request.Path,
                requestMethod: context.Request.Method,
                responseStatusCode: context.Response.StatusCode,
                executionTimeMs: executionTimeMs);

            // Set user information if available
            if (!string.IsNullOrEmpty(userName))
            {
                auditLog.SetUserInfo(userId, userName);
            }

            await auditService.LogAsync(auditLog);
        }
        catch (Exception ex)
        {
            // Don't let audit logging failures break the request
            _logger.LogError(ex, "Failed to log audit information for request {CorrelationId}", correlationId);
        }
    }

    private static AuditAction GetAuditActionFromHttpMethod(string httpMethod)
    {
        return httpMethod.ToUpperInvariant() switch
        {
            "GET" => AuditAction.View,
            "POST" => AuditAction.Insert,
            "PUT" => AuditAction.Update,
            "PATCH" => AuditAction.Update,
            "DELETE" => AuditAction.Delete,
            _ => AuditAction.View
        };
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(firstIp))
                return firstIp;
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Extension methods for registering audit middleware
/// </summary>
public static class AuditMiddlewareExtensions
{
    /// <summary>
    /// Register the audit middleware in the request pipeline
    /// </summary>
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditMiddleware>();
    }
}
