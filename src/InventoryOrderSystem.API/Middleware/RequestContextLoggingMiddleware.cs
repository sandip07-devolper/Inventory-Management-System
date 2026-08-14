using System.Security.Claims;
using Serilog.Context;

namespace InventoryOrderSystem.API.Middleware;

/// <summary>
/// Enriches every log entry written during this request with the caller's
/// TenantId and UserId (once authenticated), so logs can be filtered per
/// tenant without any service having to pass that context around manually.
/// </summary>
public class RequestContextLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.User.FindFirst("tenantId")?.Value ?? "anonymous";
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("UserId", userId))
        {
            await _next(context);
        }
    }
}
