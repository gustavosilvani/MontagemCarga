using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MontagemCarga.Domain.Enums;
using MontagemCarga.Domain.ValueObjects;

namespace MontagemCarga.Infrastructure.Services.Planning;

public sealed class OsrmRoutingProvider : IRoutingProvider
{
    private const int HoraBaseRoteirizacaoUtc = 8;

    private readonly HttpClient _httpClient;
    private readonly ILogger<OsrmRoutingProvider> _logger;
    private readonly OsrmOptions _options;

    public OsrmRoutingProvider(
        HttpClient httpClient,
        IOptions<OsrmOptions> options,
        ILogger<OsrmRoutingProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<RouteBuildResult?> BuildRouteAsync(
        IReadOnlyList<RouteStopCandidate> paradas,
        ParametrosAgrupamentoInput parametros,
        CancellationToken cancellationToken = default)
    {
        if (paradas.Count == 0)
            return new RouteBuildResult(0m, 0m, null, Array.Empty<RouteStopPlan>());

        if (!parametros.LatitudeCentro.HasValue || !parametros.LongitudeCentro.HasValue)
            return null;

        if (paradas.Any(p => !p.Latitude.HasValue || !p.Longitude.HasValue))
            return null;

        var coordinateStops = paradas
            .Select((stop, index) => new IndexedStop(index + 1, stop))
            .ToList();

        var table = await GetTableAsync(parametros, coordinateStops.Select(s => s.Stop).ToList(), cancellationToken);
        if (table is null)
            return null;

        var orderedStops = BuildStopOrder(table, coordinateStops, parametros);
        if (orderedStops is null)
            return null;

        var route = await GetRouteAsync(parametros, orderedStops.Select(s => s.Stop).ToList(), cancellationToken);
        if (route is null)
            return null;

        var plans = new List<RouteStopPlan>(orderedStops.Count);
        var currentTime = BuildBaseDeparture(parametros.DataPrevistaCarregamento);
        decimal duracaoTotal = 0m;

        for (var index = 0; index < orderedStops.Count; index++)
        {
            var stop = orderedStops[index].Stop;
            var leg = route.Legs[index];
            var travelMinutes = leg.DurationMinutes;
            var distanceKm = leg.DistanceKm;
            var arrivalUtc = currentTime.AddMinutes((double)travelMinutes);
            var waitMinutes = 0m;

            if (parametros.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.VrpTimeWindows)
            {
                var windowStart = RoutePlanner.NormalizeUtc(stop.JanelaEntregaInicioUtc ?? parametros.DataPrevistaCarregamento.Date);
                var windowEnd = RoutePlanner.NormalizeUtc(stop.JanelaEntregaFimUtc ?? parametros.DataPrevistaCarregamento.Date.AddDays(1).AddMinutes(-1));
                var tolerancia = Math.Max(0, parametros.ConfiguracaoRoteirizacao?.ToleranciaJanelaMin ?? 0);

                if (arrivalUtc > windowEnd.AddMinutes(tolerancia))
                    return null;

                if (arrivalUtc < windowStart)
                {
                    waitMinutes = decimal.Round((decimal)(windowStart - arrivalUtc).TotalMinutes, 2, MidpointRounding.AwayFromZero);
                    arrivalUtc = windowStart;
                }
            }

            var tempoServico = Math.Max(0, stop.TempoServicoMinutos > 0
                ? stop.TempoServicoMinutos
                : parametros.ConfiguracaoRoteirizacao?.TempoParadaPadraoMin ?? 0);

            var departureUtc = arrivalUtc.AddMinutes(tempoServico);
            plans.Add(new RouteStopPlan(
                stop.PedidoCodigo,
                stop.Latitude!.Value,
                stop.Longitude!.Value,
                index + 1,
                arrivalUtc,
                departureUtc,
                distanceKm,
                travelMinutes));

            duracaoTotal += travelMinutes + waitMinutes + tempoServico;
            currentTime = departureUtc;
        }

        if (route.Legs.Count > orderedStops.Count)
            duracaoTotal += route.Legs[^1].DurationMinutes;

        return new RouteBuildResult(route.TotalDistanceKm, decimal.Round(duracaoTotal, 2, MidpointRounding.AwayFromZero), route.Geometry, plans);
    }

    private async Task<TableResult?> GetTableAsync(
        ParametrosAgrupamentoInput parametros,
        IReadOnlyList<RouteStopCandidate> stops,
        CancellationToken cancellationToken)
    {
        var coordinates = BuildCoordinateList(parametros, stops, includeReturnToOrigin: false);
        var uri = $"table/v1/{_options.Profile}/{coordinates}?annotations=distance,duration";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<OsrmTableResponse>(uri, cancellationToken);
            if (response?.Durations is null || response.Distances is null)
                return null;

            return new TableResult(
                response.Durations.Select(row => row.Select(value => RoutePlanner.ToDecimal((value ?? 0d) / 60d)).ToArray()).ToArray(),
                response.Distances.Select(row => row.Select(value => RoutePlanner.ToDecimal((value ?? 0d) / 1000d)).ToArray()).ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar matriz OSRM.");
            return null;
        }
    }

    private async Task<RouteResponse?> GetRouteAsync(
        ParametrosAgrupamentoInput parametros,
        IReadOnlyList<RouteStopCandidate> orderedStops,
        CancellationToken cancellationToken)
    {
        var coordinates = BuildCoordinateList(parametros, orderedStops, includeReturnToOrigin: true);
        var uri = $"route/v1/{_options.Profile}/{coordinates}?overview=full&geometries=polyline&steps=false";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<OsrmRouteResponse>(uri, cancellationToken);
            var route = response?.Routes?.FirstOrDefault();
            if (route is null || route.Legs is null)
                return null;

            return new RouteResponse(
                RoutePlanner.ToDecimal(route.Distance / 1000d),
                route.Geometry,
                route.Legs.Select(leg => new RouteLeg(
                    RoutePlanner.ToDecimal(leg.Distance / 1000d),
                    RoutePlanner.ToDecimal(leg.Duration / 60d))).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar rota OSRM.");
            return null;
        }
    }

    private static List<IndexedStop>? BuildStopOrder(
        TableResult table,
        IReadOnlyList<IndexedStop> stops,
        ParametrosAgrupamentoInput parametros)
    {
        var remaining = stops.ToList();
        var ordered = new List<IndexedStop>(stops.Count);
        var currentMatrixIndex = 0;
        var currentTime = BuildBaseDeparture(parametros.DataPrevistaCarregamento);

        while (remaining.Count > 0)
        {
            var evaluated = remaining
                .Select(stop => EvaluateStop(table, currentMatrixIndex, stop, currentTime, parametros))
                .Where(item => item.IsFeasible)
                .OrderBy(item => item.CostRank)
                .ThenBy(item => item.DistanceKm)
                .ThenBy(item => item.Stop.Stop.CanalEntregaPrioridade ?? 999)
                .ThenBy(item => item.Stop.Stop.PedidoCodigo, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (evaluated.Count == 0)
                return null;

            var chosen = evaluated.First();
            ordered.Add(chosen.Stop);
            currentMatrixIndex = chosen.Stop.MatrixIndex;
            currentTime = chosen.DepartureUtc;
            remaining.Remove(chosen.Stop);
        }

        return ordered;
    }

    private static EvaluatedStop EvaluateStop(
        TableResult table,
        int currentMatrixIndex,
        IndexedStop stop,
        DateTime currentTime,
        ParametrosAgrupamentoInput parametros)
    {
        var travelMinutes = table.Durations[currentMatrixIndex][stop.MatrixIndex];
        var distanceKm = table.Distances[currentMatrixIndex][stop.MatrixIndex];
        var arrivalUtc = currentTime.AddMinutes((double)travelMinutes);
        var waitMinutes = 0m;

        if (parametros.TipoMontagemCarregamentoVRP == TipoMontagemCarregamentoVRP.VrpTimeWindows)
        {
            var windowStart = RoutePlanner.NormalizeUtc(stop.Stop.JanelaEntregaInicioUtc ?? parametros.DataPrevistaCarregamento.Date);
            var windowEnd = RoutePlanner.NormalizeUtc(stop.Stop.JanelaEntregaFimUtc ?? parametros.DataPrevistaCarregamento.Date.AddDays(1).AddMinutes(-1));
            var tolerancia = Math.Max(0, parametros.ConfiguracaoRoteirizacao?.ToleranciaJanelaMin ?? 0);

            if (arrivalUtc > windowEnd.AddMinutes(tolerancia))
                return EvaluatedStop.Infeasible(stop);

            if (arrivalUtc < windowStart)
            {
                waitMinutes = decimal.Round((decimal)(windowStart - arrivalUtc).TotalMinutes, 2, MidpointRounding.AwayFromZero);
                arrivalUtc = windowStart;
            }
        }

        var serviceMinutes = Math.Max(0, stop.Stop.TempoServicoMinutos > 0
            ? stop.Stop.TempoServicoMinutos
            : parametros.ConfiguracaoRoteirizacao?.TempoParadaPadraoMin ?? 0);

        var departureUtc = arrivalUtc.AddMinutes(serviceMinutes);
        return EvaluatedStop.Feasible(stop, arrivalUtc, departureUtc, distanceKm, travelMinutes, waitMinutes + travelMinutes);
    }

    private static string BuildCoordinateList(ParametrosAgrupamentoInput parametros, IReadOnlyList<RouteStopCandidate> stops, bool includeReturnToOrigin)
    {
        var coordinates = new List<string>
        {
            $"{parametros.LongitudeCentro!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{parametros.LatitudeCentro!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
        };

        coordinates.AddRange(stops.Select(stop =>
            $"{stop.Longitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{stop.Latitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

        if (includeReturnToOrigin)
        {
            coordinates.Add(
                $"{parametros.LongitudeCentro!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{parametros.LatitudeCentro!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        return string.Join(';', coordinates);
    }

    private static DateTime BuildBaseDeparture(DateTime dataPrevista) =>
        RoutePlanner.NormalizeUtc(dataPrevista.Date.AddHours(HoraBaseRoteirizacaoUtc));

    private sealed record IndexedStop(int MatrixIndex, RouteStopCandidate Stop);

    private sealed record TableResult(decimal[][] Durations, decimal[][] Distances);

    private sealed record RouteLeg(decimal DistanceKm, decimal DurationMinutes);

    private sealed record RouteResponse(decimal TotalDistanceKm, string? Geometry, IReadOnlyList<RouteLeg> Legs);

    private sealed record EvaluatedStop(
        bool IsFeasible,
        IndexedStop Stop,
        DateTime DepartureUtc,
        decimal DistanceKm,
        decimal TravelMinutes,
        decimal CostRank)
    {
        public static EvaluatedStop Infeasible(IndexedStop stop) =>
            new(false, stop, default, 0m, 0m, decimal.MaxValue);

        public static EvaluatedStop Feasible(
            IndexedStop stop,
            DateTime arrivalUtc,
            DateTime departureUtc,
            decimal distanceKm,
            decimal travelMinutes,
            decimal costRank) =>
            new(true, stop, departureUtc, distanceKm, travelMinutes, costRank);
    }

    private sealed record OsrmTableResponse(
        [property: JsonPropertyName("durations")] double?[][]? Durations,
        [property: JsonPropertyName("distances")] double?[][]? Distances);

    private sealed record OsrmRouteResponse(
        [property: JsonPropertyName("routes")] List<OsrmRouteItem>? Routes);

    private sealed record OsrmRouteItem(
        [property: JsonPropertyName("distance")] double Distance,
        [property: JsonPropertyName("duration")] double Duration,
        [property: JsonPropertyName("geometry")] string? Geometry,
        [property: JsonPropertyName("legs")] List<OsrmLegItem>? Legs);

    private sealed record OsrmLegItem(
        [property: JsonPropertyName("distance")] double Distance,
        [property: JsonPropertyName("duration")] double Duration);
}
