using MontagemCarga.Domain.ValueObjects;

namespace MontagemCarga.Infrastructure.Services.Planning;

internal static class RoutePlanner
{
    private const int HoraBaseRoteirizacaoUtc = 8;

    public static List<RouteStopCandidate> AggregateStops(IReadOnlyList<PlanningUnit> unidades)
    {
        return unidades
            .GroupBy(u => u.CodigoPedido, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new RouteStopCandidate(
                    first.CodigoPedido,
                    first.Latitude,
                    first.Longitude,
                    first.CanalEntregaPrioridade,
                    first.CanalEntregaLimitePedidos,
                    group.Min(x => x.PrevisaoEntrega),
                    group.Min(x => x.JanelaEntregaInicioUtc),
                    group.Max(x => x.JanelaEntregaFimUtc),
                    group.Max(x => x.TempoServicoMinutos));
            })
            .ToList();
    }

    public static RouteBuildResult? BuildFallbackRoute(
        IReadOnlyList<RouteStopCandidate> paradas,
        ParametrosAgrupamentoInput parametros)
    {
        if (paradas.Count == 0)
            return new RouteBuildResult(0m, 0m, null, Array.Empty<RouteStopPlan>());

        var usarRotaGeo = parametros.LatitudeCentro.HasValue &&
                          parametros.LongitudeCentro.HasValue &&
                          paradas.All(p => p.Latitude.HasValue && p.Longitude.HasValue);

        return usarRotaGeo
            ? BuildGeoFallbackRoute(paradas, parametros)
            : BuildSyntheticRoute(paradas, parametros);
    }

    public static decimal ToDecimal(double value) =>
        decimal.Round((decimal)value, 4, MidpointRounding.AwayFromZero);

    public static decimal ComputePercent(decimal total, decimal capacity)
    {
        if (capacity <= 0)
            return 0m;

        return decimal.Round((total / capacity) * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal ComputePercent(int total, int capacity)
    {
        if (capacity <= 0)
            return 0m;

        return decimal.Round(((decimal)total / capacity) * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static RouteBuildResult BuildSyntheticRoute(
        IReadOnlyList<RouteStopCandidate> paradas,
        ParametrosAgrupamentoInput parametros)
    {
        var instante = BuildBaseDeparture(parametros.DataPrevistaCarregamento);
        var resultado = new List<RouteStopPlan>(paradas.Count);

        foreach (var parada in paradas
                     .OrderBy(p => p.CanalEntregaPrioridade ?? 999)
                     .ThenBy(p => p.PrevisaoEntrega ?? DateTime.MaxValue)
                     .ThenBy(p => p.PedidoCodigo, StringComparer.OrdinalIgnoreCase)
                     .Select((p, index) => (Parada: p, Ordem: index + 1)))
        {
            var tempoServico = parada.Parada.TempoServicoMinutos > 0
                ? parada.Parada.TempoServicoMinutos
                : parametros.ConfiguracaoRoteirizacao?.TempoParadaPadraoMin ?? 0;

            var chegada = instante;
            var saida = chegada.AddMinutes(tempoServico);

            resultado.Add(new RouteStopPlan(
                parada.Parada.PedidoCodigo,
                parada.Parada.Latitude ?? 0,
                parada.Parada.Longitude ?? 0,
                parada.Ordem,
                chegada,
                saida,
                0m,
                0m));

            instante = saida;
        }

        return new RouteBuildResult(0m, 0m, BuildGeometry(parametros, resultado), resultado);
    }

    private static RouteBuildResult BuildGeoFallbackRoute(
        IReadOnlyList<RouteStopCandidate> paradas,
        ParametrosAgrupamentoInput parametros)
    {
        var velocidade = Math.Max(1m, parametros.ConfiguracaoRoteirizacao?.VelocidadeMediaKmH ?? 45m);
        var tempoParadaDefault = Math.Max(0, parametros.ConfiguracaoRoteirizacao?.TempoParadaPadraoMin ?? 0);
        var currentLat = parametros.LatitudeCentro!.Value;
        var currentLon = parametros.LongitudeCentro!.Value;
        var currentTime = BuildBaseDeparture(parametros.DataPrevistaCarregamento);
        decimal distanciaTotal = 0m;
        decimal duracaoTotal = 0m;
        var ordem = 1;
        var restantes = paradas.ToList();
        var resultado = new List<RouteStopPlan>(paradas.Count);

        while (restantes.Count > 0)
        {
            var escolhido = restantes
                .Select(stop =>
                {
                    var distanceKm = ToDecimal(HaversineKm(currentLat, currentLon, stop.Latitude!.Value, stop.Longitude!.Value));
                    var travelMinutes = decimal.Round((distanceKm / velocidade) * 60m, 2, MidpointRounding.AwayFromZero);
                    return new { Stop = stop, DistanceKm = distanceKm, TravelMinutes = travelMinutes };
                })
                .OrderBy(x => x.TravelMinutes)
                .ThenBy(x => x.Stop.CanalEntregaPrioridade ?? 999)
                .ThenBy(x => x.Stop.PedidoCodigo, StringComparer.OrdinalIgnoreCase)
                .First();

            var chegada = currentTime.AddMinutes((double)escolhido.TravelMinutes);
            var tempoServico = Math.Max(0, escolhido.Stop.TempoServicoMinutos > 0 ? escolhido.Stop.TempoServicoMinutos : tempoParadaDefault);
            var saida = chegada.AddMinutes(tempoServico);

            resultado.Add(new RouteStopPlan(
                escolhido.Stop.PedidoCodigo,
                escolhido.Stop.Latitude!.Value,
                escolhido.Stop.Longitude!.Value,
                ordem++,
                chegada,
                saida,
                escolhido.DistanceKm,
                escolhido.TravelMinutes));

            distanciaTotal += escolhido.DistanceKm;
            duracaoTotal += escolhido.TravelMinutes + tempoServico;
            currentLat = escolhido.Stop.Latitude.Value;
            currentLon = escolhido.Stop.Longitude.Value;
            currentTime = saida;
            restantes.Remove(escolhido.Stop);
        }

        var distanciaRetorno = ToDecimal(HaversineKm(currentLat, currentLon, parametros.LatitudeCentro.Value, parametros.LongitudeCentro.Value));
        var duracaoRetorno = decimal.Round((distanciaRetorno / velocidade) * 60m, 2, MidpointRounding.AwayFromZero);

        return new RouteBuildResult(
            distanciaTotal + distanciaRetorno,
            duracaoTotal + duracaoRetorno,
            BuildGeometry(parametros, resultado),
            resultado);
    }

    public static string? BuildGeometry(ParametrosAgrupamentoInput parametros, IReadOnlyList<RouteStopPlan> paradas)
    {
        if (!parametros.LatitudeCentro.HasValue || !parametros.LongitudeCentro.HasValue)
            return null;

        var points = new List<(double Latitude, double Longitude)>
        {
            (parametros.LatitudeCentro.Value, parametros.LongitudeCentro.Value)
        };

        points.AddRange(paradas.Select(p => (p.Latitude, p.Longitude)));
        if (paradas.Count > 0)
            points.Add((parametros.LatitudeCentro.Value, parametros.LongitudeCentro.Value));

        return EncodePolyline(points);
    }

    private static DateTime BuildBaseDeparture(DateTime dataPrevista) =>
        NormalizeUtc(dataPrevista.Date.AddHours(HoraBaseRoteirizacaoUtc));

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6371d;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(DegreesToRadians(lat1)) *
                Math.Cos(DegreesToRadians(lat2)) *
                Math.Pow(Math.Sin(dLon / 2), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return radius * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180d);

    private static string EncodePolyline(IEnumerable<(double Latitude, double Longitude)> points)
    {
        var result = new System.Text.StringBuilder();
        var prevLat = 0;
        var prevLng = 0;

        foreach (var (latitude, longitude) in points)
        {
            var lat = (int)Math.Round(latitude * 1e5);
            var lng = (int)Math.Round(longitude * 1e5);
            EncodeSigned(lat - prevLat, result);
            EncodeSigned(lng - prevLng, result);
            prevLat = lat;
            prevLng = lng;
        }

        return result.ToString();
    }

    private static void EncodeSigned(int value, System.Text.StringBuilder result)
    {
        var sgnNum = value << 1;
        if (value < 0)
            sgnNum = ~sgnNum;

        while (sgnNum >= 0x20)
        {
            result.Append((char)((0x20 | (sgnNum & 0x1f)) + 63));
            sgnNum >>= 5;
        }

        result.Append((char)(sgnNum + 63));
    }
}
