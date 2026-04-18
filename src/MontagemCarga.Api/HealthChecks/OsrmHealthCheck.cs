using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MontagemCarga.Infrastructure.Services.Planning;

namespace MontagemCarga.Api.HealthChecks;

public sealed class OsrmHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly OsrmOptions _options;

    public OsrmHealthCheck(HttpClient httpClient, IOptions<OsrmOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            return HealthCheckResult.Degraded("OSRM nao configurado.");

        try
        {
            using var response = await _httpClient.GetAsync("nearest/v1/driving/0,0", cancellationToken);
            return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? HealthCheckResult.Healthy("OSRM acessivel.")
                : HealthCheckResult.Degraded($"OSRM respondeu {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Falha ao consultar OSRM.", ex);
        }
    }
}
