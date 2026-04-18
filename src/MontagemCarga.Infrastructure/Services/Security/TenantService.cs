using Microsoft.AspNetCore.Http;
using MontagemCarga.Domain.Interfaces;
using System.Security.Claims;

namespace MontagemCarga.Infrastructure.Services.Security;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TenantIdKey = "TenantId";
    private const string TenantIdHeader = "X-Tenant-Id";

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? ObterEmbarcadorIdAtual()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Items.TryGetValue(TenantIdKey, out var tenantValue) && tenantValue is Guid tenantId)
            return tenantId;

        if (httpContext?.Request.Headers.TryGetValue(TenantIdHeader, out var tenantIdHeader) == true &&
            Guid.TryParse(tenantIdHeader, out var tenantIdFromHeader))
            return tenantIdFromHeader;

        var claim = httpContext?.User?.FindFirst("EmbarcadorId")?.Value;
        if (!string.IsNullOrWhiteSpace(claim) && Guid.TryParse(claim, out var embarcadorId))
            return embarcadorId;

        return null;
    }

    public string? ObterOperadorIdAtual()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext?.User?.FindFirstValue("sub")
            ?? httpContext?.User?.FindFirstValue("preferred_username");
    }

    public string? ObterNomeOperadorAtual()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.FindFirstValue(ClaimTypes.Name)
            ?? httpContext?.User?.FindFirstValue("name")
            ?? httpContext?.User?.FindFirstValue("preferred_username")
            ?? ObterOperadorIdAtual();
    }

    public void DefinirEmbarcadorId(Guid embarcadorId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
            httpContext.Items[TenantIdKey] = embarcadorId;
    }
}
