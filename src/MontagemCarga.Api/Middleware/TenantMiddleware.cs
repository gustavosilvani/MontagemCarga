using MontagemCarga.Domain.Interfaces;

namespace MontagemCarga.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        if (TryResolveTenant(context, out var embarcadorId))
            tenantService.DefinirEmbarcadorId(embarcadorId);

        var requiresTenant =
            context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
            context.User.Identity?.IsAuthenticated == true;

        if (requiresTenant && tenantService.ObterEmbarcadorIdAtual() == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Contexto do embarcador nao informado.",
                errors = new[] { "Envie X-Tenant-Id valido ou autentique-se com um token que contenha a claim EmbarcadorId." }
            });
            return;
        }

        await _next(context);
    }

    private static bool TryResolveTenant(HttpContext context, out Guid embarcadorId)
    {
        embarcadorId = Guid.Empty;

        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader) &&
            Guid.TryParse(tenantIdHeader, out embarcadorId))
        {
            return true;
        }

        var claim = context.User?.FindFirst("EmbarcadorId")?.Value;
        return !string.IsNullOrWhiteSpace(claim) && Guid.TryParse(claim, out embarcadorId);
    }
}
