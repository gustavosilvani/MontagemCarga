using MontagemCarga.Domain.ValueObjects;

namespace MontagemCarga.Infrastructure.Services.Planning;

public interface IRoutingProvider
{
    Task<RouteBuildResult?> BuildRouteAsync(
        IReadOnlyList<RouteStopCandidate> paradas,
        ParametrosAgrupamentoInput parametros,
        CancellationToken cancellationToken = default);
}
